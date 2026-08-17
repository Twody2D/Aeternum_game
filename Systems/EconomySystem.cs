using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Производство и потребление еды — по каждому поселению отдельно, они не делятся
// запасами друг с другом. Взрослые работники кормят всё живое население поселения
// по своей профессии (см. ProfessionSystem.GetFoodProduction). При устойчивом
// дефиците часть жителей гибнет от голода (DeathReason.Starvation)
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
            .GroupBy(c => c.Settlement!);

        foreach (var group in aliveBySettlement)
        {
            ProcessSettlement(group.Key, group.ToList(), world);
        }
    }

    private static void ProcessSettlement(Settlement settlement, List<Character> residents, World world)
    {
        double production = residents.Sum(c =>
            ProfessionSystem.GetFoodProduction(c.Profession) * GetProductivity(c.LifeStage));

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

    // Доля полноценной выработки в зависимости от возраста: работают в основном взрослые,
    // старики и ученики помогают лишь частично, дети не производят ничего
    private static double GetProductivity(LifeStage stage)
    {
        return stage switch
        {
            LifeStage.Adult => 1.0,
            LifeStage.Elder => 0.5,
            LifeStage.Student => 0.3,
            _ => 0.0
        };
    }
}
