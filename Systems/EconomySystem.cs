using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Производство и потребление еды, производство материалов по типам — по каждому
// поселению отдельно (сглаживание между поселениями одного государства — см. TradeSystem).
// Взрослые работники кормят всё живое население поселения по своей профессии
// (см. ProfessionSystem.GetFoodProduction/GetMaterialProduction). При устойчивом
// дефиците еды часть жителей гибнет от голода (DeathReason.Starvation); материалы
// пока только копятся — тратить их не на что
public static class EconomySystem
{
    private static readonly Random _random = new();

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
        }
    }

    private static void ProcessSettlement(Settlement settlement, List<Character> residents, World world)
    {
        double production = residents.Sum(c =>
            ProfessionSystem.GetFoodProduction(c.Profession) * GetProductivity(c));

        foreach (var resident in residents)
        {
            var (type, amount) = ProfessionSystem.GetMaterialProduction(resident.Profession);
            var materialAmount = amount * GetProductivity(resident);

            if (materialAmount <= 0)
            {
                continue;
            }

            settlement.MaterialStocks[type] = settlement.MaterialStocks.GetValueOrDefault(type) + materialAmount;
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
            if (_random.NextDouble() < starvationChance)
            {
                DeathSystem.Kill(character, world, DeathReason.Starvation);
            }
        }

        // Голод не копится бесконечно: население адаптируется (сокращает потребление,
        // получает помощь и т.п.), поэтому запас не проваливается глубже одного года потребления
        settlement.FoodStock = Math.Max(settlement.FoodStock, -consumption);
    }

    private const double HardworkingMultiplier = 1.25;

    // Доля полноценной выработки в зависимости от возраста: работают в основном взрослые,
    // старики и ученики помогают лишь частично, дети не производят ничего.
    // Трудолюбивые (см. Trait) дают надбавку сверху
    private static double GetProductivity(Character character)
    {
        double baseProductivity = character.LifeStage switch
        {
            LifeStage.Adult => 1.0,
            LifeStage.Elder => 0.5,
            LifeStage.Student => 0.3,
            _ => 0.0
        };

        return character.Traits.Contains(Trait.Hardworking) ? baseProductivity * HardworkingMultiplier : baseProductivity;
    }
}
