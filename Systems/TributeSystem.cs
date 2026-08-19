using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Дань: государство забирает часть излишков (еды и материалов) подконтрольных
// поселений в свою казну. В отличие от TradeSystem (сглаживает дефицит/излишек
// между поселениями) — казна не возвращается провинциям, а определяет
// устойчивость самого государства (см. KingdomSystem.TryTriggerSuccessionCrisis)
public static class TributeSystem
{
    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            foreach (var settlement in kingdom.Settlements)
            {
                if (settlement.FoodStock > 0)
                {
                    var foodTribute = settlement.FoodStock * world.Settings.TributeRate;
                    settlement.FoodStock -= foodTribute;
                    kingdom.FoodTreasury += foodTribute;
                }

                if (settlement.Gold > 0)
                {
                    var goldTribute = settlement.Gold * world.Settings.TributeRate;
                    settlement.Gold -= goldTribute;
                    kingdom.GoldTreasury += goldTribute;
                }

                foreach (var type in Enum.GetValues<MaterialType>())
                {
                    var stock = settlement.MaterialStocks.GetValueOrDefault(type);

                    if (stock <= 0)
                    {
                        continue;
                    }

                    var materialTribute = stock * world.Settings.TributeRate;
                    settlement.MaterialStocks[type] = stock - materialTribute;
                    kingdom.MaterialTreasury[type] = kingdom.MaterialTreasury.GetValueOrDefault(type) + materialTribute;
                }
            }
        }
    }
}
