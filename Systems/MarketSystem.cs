using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// То, чего бартер (TradeSystem) не может: поселение с дефицитом еды после
// торговли с союзниками покупает недостающее на внешнем рынке за накопленное
// золото (производится профессиями Trade — см. ProfessionSystem.GetGoldProduction).
// Работает независимо от союзов и соседей — единственная уникальная польза золота,
// не сводящаяся к обычному перераспределению
public static class MarketSystem
{
    private const double FoodPerGold = 2.0; // Обменный курс внешнего рынка

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            if (settlement.FoodStock >= 0 || settlement.Gold <= 0)
            {
                continue;
            }

            var neededFood = -settlement.FoodStock;
            var affordableFood = settlement.Gold * FoodPerGold;
            var boughtFood = Math.Min(neededFood, affordableFood);
            var goldSpent = boughtFood / FoodPerGold;

            settlement.Gold -= goldSpent;
            settlement.FoodStock += boughtFood;
        }
    }
}
