using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Первый шаг фазы "эволюция": поселения сами строят дома по мере роста
// населения и накопления материалов (дерево + камень) — без участия
// персонажа-инициатора, в отличие от колонизации. Обжитое поселение
// безопаснее (ниже риск несчастного случая) и стабильнее (меньше миграции)
public static class HousingSystem
{
    private const double WoodCost = 15;
    private const double StoneCost = 10;

    private const int ResidentsPerHouse = 5; // Целевое число домов растёт вместе с населением

    private const double MaxHousingBonus = 0.3; // Максимум -30% к риску/шансу при достаточном числе домов
    private const double BonusPerHouse = 0.05;

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var alivePopulation = settlement.Members.Count(m => m.Alive);

            if (alivePopulation == 0)
            {
                continue;
            }

            var targetHouses = (int)Math.Ceiling(alivePopulation / (double)ResidentsPerHouse);

            if (settlement.Houses >= targetHouses)
            {
                continue;
            }

            var wood = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Wood);
            var stone = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Stone);

            if (wood < WoodCost || stone < StoneCost)
            {
                continue;
            }

            settlement.MaterialStocks[MaterialType.Wood] = wood - WoodCost;
            settlement.MaterialStocks[MaterialType.Stone] = stone - StoneCost;
            settlement.Houses++;
        }
    }

    // Понижающий множитель для риска/шанса (1.0 — домов нет, ниже — чем больше домов)
    public static double GetHousingFactor(Settlement? settlement)
    {
        if (settlement == null)
        {
            return 1.0;
        }

        var bonus = Math.Min(MaxHousingBonus, settlement.Houses * BonusPerHouse);

        return 1 - bonus;
    }
}
