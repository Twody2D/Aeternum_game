using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Пятый и последний шаг фазы "эволюция": стены отвечают за оборону — снижают
// потери, когда спор за поселение между государствами перерастает в войну
// (см. WarSystem). Строятся из камня и металла (кладка + окованные врата),
// самое редкое и крупное вложение наравне со школами
public static class WallSystem
{
    private const double StoneCost = 25;
    private const double MetalCost = 20;

    private const int ResidentsPerWall = 25; // Целевое число стен растёт вместе с населением, так же редко, как школы

    private const double MaxWallBonus = 0.5; // Максимум -50% потерь при войне — осаждённое поселение реально может отбиться
    private const double BonusPerWall = 0.1;

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var alivePopulation = settlement.Members.Count(m => m.Alive);

            if (alivePopulation == 0)
            {
                continue;
            }

            var targetWalls = (int)Math.Ceiling(alivePopulation / (double)ResidentsPerWall);

            if (settlement.Walls >= targetWalls)
            {
                continue;
            }

            var stone = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Stone);
            var metal = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Metal);

            if (stone < StoneCost || metal < MetalCost)
            {
                continue;
            }

            settlement.MaterialStocks[MaterialType.Stone] = stone - StoneCost;
            settlement.MaterialStocks[MaterialType.Metal] = metal - MetalCost;
            settlement.Walls++;
        }
    }

    // Понижающий множитель для потерь при войне (1.0 — стен нет, ниже — чем больше стен)
    public static double GetWallFactor(Settlement? settlement)
    {
        if (settlement == null)
        {
            return 1.0;
        }

        var bonus = Math.Min(MaxWallBonus, settlement.Walls * BonusPerWall);

        return 1 - bonus;
    }
}
