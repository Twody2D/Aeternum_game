using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Четвёртый шаг фазы "эволюция": школы отвечают за профессию, а не за
// демографию (дома/больницы) или материалы (мастерские) — повышают шанс,
// что подросток в 16 лет станет учёным/лекарем/магом и т.п. (категория
// Knowledge). Строятся из дерева и металла (парты, письменные приборы),
// реже больниц — ещё более крупное вложение. Сам эффект считается прямо
// в ProfessionSystem.GetRandom, здесь — только строительство
public static class SchoolSystem
{
    private const double WoodCost = 20;
    private const double MetalCost = 15;

    private const int ResidentsPerSchool = 20; // Целевое число школ растёт вместе с населением, медленнее больниц

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var alivePopulation = settlement.Members.Count(m => m.Alive);

            if (alivePopulation == 0)
            {
                continue;
            }

            var targetSchools = (int)Math.Ceiling(alivePopulation / (double)ResidentsPerSchool);

            if (settlement.Schools >= targetSchools)
            {
                continue;
            }

            var wood = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Wood);
            var metal = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Metal);

            if (wood < WoodCost || metal < MetalCost)
            {
                continue;
            }

            settlement.MaterialStocks[MaterialType.Wood] = wood - WoodCost;
            settlement.MaterialStocks[MaterialType.Metal] = metal - MetalCost;
            settlement.Schools++;
        }
    }
}
