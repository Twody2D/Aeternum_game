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
            // Опустевшее поселение не пропускаем — брошенное должно доветшать (см. HousingSystem)
            var alivePopulation = settlement.Members.Count(m => m.Alive);

            var targetWalls = (int)Math.Ceiling(alivePopulation / (double)ResidentsPerWall);

            if (settlement.Walls >= targetWalls)
            {
                if (DecaySystem.ShouldDecay(settlement.Walls, targetWalls, world))
                {
                    settlement.Walls--; // Некому держать оборону — кладка осыпается
                }

                continue;
            }

            var stone = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Stone);
            var metal = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Metal);

            var discount = TechnologySystem.GetBuildCostMultiplier(world); // См. HousingSystem
            var stoneCost = StoneCost * discount;
            var metalCost = MetalCost * discount;

            if (stone < stoneCost || metal < metalCost)
            {
                continue;
            }

            settlement.MaterialStocks[MaterialType.Stone] = stone - stoneCost;
            settlement.MaterialStocks[MaterialType.Metal] = metal - metalCost;
            settlement.Walls++;
        }
    }

    // Понижающий множитель для потерь при войне (1.0 — стен нет, ниже — чем больше
    // стен). Накопленное знание усиливает отдачу той же кладки — стена это прежде
    // всего инженерное сооружение, и с веками её умеют строить лучше. Тот же приём,
    // что уже применён к больницам (HospitalSystem.GetHospitalFactor)
    public static double GetWallFactor(Settlement? settlement, World world)
    {
        if (settlement == null)
        {
            return 1.0;
        }

        var bonus = Math.Min(MaxWallBonus, settlement.Walls * BonusPerWall * TechnologySystem.GetProductionMultiplier(world));

        return 1 - bonus;
    }
}
