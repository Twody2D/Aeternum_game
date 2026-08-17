using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Производство и потребление еды: работающее население кормит всё живое по
// своей профессии (см. ProfessionSystem.GetFoodProduction). При устойчивом
// дефиците часть населения гибнет от голода (DeathReason.Starvation)
public static class EconomySystem
{
    private static readonly Random _random = new();

    // Максимальная доля годового дефицита, учитываемая при расчёте риска голода —
    // не даёт одному тяжёлому году выкосить почти всё население разом
    private const double MaxDeficitRatio = 0.5;

    public static void Process(World world)
    {
        var alive = world.Characters.Where(c => c.Alive).ToList();

        if (alive.Count == 0)
        {
            return;
        }

        double production = alive.Sum(c =>
            ProfessionSystem.GetFoodProduction(c.Profession) * GetProductivity(c.LifeStage));

        double consumption = alive.Count * world.Settings.FoodConsumptionPerCapita;

        world.FoodStock += production - consumption;

        if (world.FoodStock >= 0)
        {
            return;
        }

        // Чем глубже дефицит относительно годового потребления, тем выше шанс голодной смерти
        double deficitRatio = Math.Min(MaxDeficitRatio, -world.FoodStock / consumption);
        double starvationChance = deficitRatio * world.Settings.StarvationSeverity;

        foreach (var character in alive)
        {
            if (_random.NextDouble() < starvationChance)
            {
                DeathSystem.Kill(character, world, DeathReason.Starvation);
            }
        }

        // Голод не копится бесконечно: население адаптируется (сокращает потребление,
        // получает помощь и т.п.), поэтому запас не проваливается глубже одного года потребления
        world.FoodStock = Math.Max(world.FoodStock, -consumption);
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
