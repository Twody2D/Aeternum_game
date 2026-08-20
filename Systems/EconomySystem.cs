using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Производство и потребление еды, производство материалов по типам — по каждому
// поселению отдельно (сглаживание между поселениями одного государства — см. TradeSystem).
// Взрослые работники кормят всё живое население поселения по своей профессии
// (см. ProfessionSystem.GetFoodProduction/GetMaterialProduction). При устойчивом
// дефиците еды часть жителей гибнет от голода (DeathReason.Starvation); излишки
// же не копятся бесконечно — их ограничивают склады и порча (см. StorageSystem)
public static class EconomySystem
{
    // Максимальная доля годового дефицита, учитываемая при расчёте риска голода —
    // не даёт одному тяжёлому году выкосить почти всё население поселения разом
    private const double MaxDeficitRatio = 0.5;

    public static void Process(World world)
    {
        var aliveBySettlement = world.Characters
            .Where(c => c.Alive && c.Settlement != null)
            .GroupBy(c => c.Settlement!)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var settlement in world.Settlements)
        {
            if (aliveBySettlement.TryGetValue(settlement, out var residents))
            {
                ProcessSettlement(settlement, residents, world);
                continue;
            }

            // Опустевшее поселение ничего не производит и не потребляет — не должно
            // навсегда застревать с замороженным глубоко отрицательным запасом,
            // который отпугивал бы MigrationSystem от единственного способа снова
            // его заселить (выбор направления идёт по FoodStock)
            if (settlement.FoodStock != 0)
            {
                settlement.FoodStock = 0;
            }

            if (settlement.MaterialStocks.Count > 0)
            {
                settlement.MaterialStocks.Clear();
            }

            if (settlement.Gold != 0)
            {
                settlement.Gold = 0;
            }
        }
    }

    private static void ProcessSettlement(Settlement settlement, List<Character> residents, World world)
    {
        // Землю пашут одинаково усердно везде, но родит она по-разному —
        // плодородие зависит от того, где поселение стоит (см. ClimateSystem)
        // и каким выдался сам год (см. WeatherSystem), а накопленное миром
        // знание одинаково помогает всем (см. TechnologySystem)
        double production = residents.Sum(c =>
            ProfessionSystem.GetFoodProduction(c.Profession) * GetProductivity(c, world))
            * ClimateSystem.GetFertility(settlement)
            * WeatherSystem.GetFactor(world)
            * TechnologySystem.GetProductionMultiplier(world);

        settlement.Gold += residents.Sum(c =>
            ProfessionSystem.GetGoldProduction(c.Profession) * GetProductivity(c, world));

        foreach (var resident in residents)
        {
            var (type, amount) = ProfessionSystem.GetMaterialProduction(resident.Profession);

            if (type == null)
            {
                continue; // Профессия не ремесленная — сырья от неё нет
            }

            var materialAmount = amount * GetProductivity(resident, world) * WorkshopSystem.GetProductionMultiplier(settlement, type.Value);

            if (materialAmount <= 0)
            {
                continue;
            }

            settlement.MaterialStocks[type.Value] = settlement.MaterialStocks.GetValueOrDefault(type.Value) + materialAmount;
        }

        double consumption = residents.Count * world.Settings.FoodConsumptionPerCapita;

        settlement.FoodStock += production - consumption;

        if (settlement.FoodStock >= 0)
        {
            return;
        }

        // Чем глубже дефицит относительно годового потребления, тем выше шанс голодной смерти
        double deficitRatio = Math.Min(MaxDeficitRatio, -settlement.FoodStock / consumption);
        double starvationChance = deficitRatio * world.Settings.StarvationSeverity;

        foreach (var character in residents)
        {
            // Недород бьёт по всем, но не поровну: до чужих запасов и связей
            // нужда добирается последней (см. EstateSystem)
            if (Rng.NextDouble() < starvationChance * EstateSystem.GetStarvationShield(character, world))
            {
                DeathSystem.Kill(character, world, DeathReason.Starvation);
            }
        }

        // Голод не копится бесконечно: население адаптируется (сокращает потребление,
        // получает помощь и т.п.), поэтому запас не проваливается глубже одного года потребления
        settlement.FoodStock = Math.Max(settlement.FoodStock, -consumption);
    }

    private const double HardworkingMultiplier = 1.25;

    // Доля полноценной выработки: сила тела зависит от возраста (работают в
    // основном взрослые, старики и ученики помогают лишь частично, дети не
    // производят ничего), умение рук — от прожитых в ремесле лет
    // (см. ProfessionSystem.GetMastery). Трудолюбивые (см. Trait) дают надбавку
    // сверху. Отсюда сам собой выходит возраст расцвета: юнец силён, да неумел,
    // мастер умел, да немощен, а лучше всех работает тот, у кого ещё есть силы
    // и уже есть навык
    private static double GetProductivity(Character character, World world)
    {
        double baseProductivity = character.LifeStage switch
        {
            LifeStage.Adult => 1.0,
            LifeStage.Elder => 0.5,
            LifeStage.Student => 0.3,
            _ => 0.0
        };

        baseProductivity *= ProfessionSystem.GetMastery(character, world);

        return character.Traits.Contains(Trait.Hardworking) ? baseProductivity * HardworkingMultiplier : baseProductivity;
    }
}
