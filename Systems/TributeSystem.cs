using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Дань: государство забирает часть излишков (еды и материалов) подконтрольных
// поселений в свою казну. В отличие от TradeSystem (сглаживает дефицит/излишек
// между поселениями) — казна не возвращается провинциям, а определяет
// устойчивость самого государства (см. KingdomSystem.TryTriggerSuccessionCrisis)
public static class TributeSystem
{
    private const double VassalTributeRate = 0.2; // Доля уже собранной в этом году казны, которую вассал платит сюзерену сверху обычной дани

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
                if (RebellionSystem.IsRebelling(settlement, world))
                {
                    continue; // Восставшие короне не платят
                }

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

        // Вассальная дань — поверх обычной, из уже собранной в этом году казны
        // вассала (казна → казна вместо поселение → казна)
        foreach (var vassal in world.Kingdoms)
        {
            if (vassal.FallenYear != null || vassal.Suzerain == null || vassal.Suzerain.FallenYear != null)
            {
                continue;
            }

            var suzerain = vassal.Suzerain;

            var foodTribute = vassal.FoodTreasury * VassalTributeRate;
            vassal.FoodTreasury -= foodTribute;
            suzerain.FoodTreasury += foodTribute;

            var goldTribute = vassal.GoldTreasury * VassalTributeRate;
            vassal.GoldTreasury -= goldTribute;
            suzerain.GoldTreasury += goldTribute;

            foreach (var type in Enum.GetValues<MaterialType>())
            {
                var stock = vassal.MaterialTreasury.GetValueOrDefault(type);

                if (stock <= 0)
                {
                    continue;
                }

                var materialTribute = stock * VassalTributeRate;
                vassal.MaterialTreasury[type] = stock - materialTribute;
                suzerain.MaterialTreasury[type] = suzerain.MaterialTreasury.GetValueOrDefault(type) + materialTribute;
            }
        }
    }
}
