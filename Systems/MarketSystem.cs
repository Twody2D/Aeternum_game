using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// То, чего бартер (TradeSystem) не может: поселение торгует с внешним миром
// за золото, независимо от союзов и соседей. Работает в обе стороны — сначала
// сбывает то, что всё равно не пережило бы зимы на складе (см. StorageSystem),
// потом на вырученное докупает недостающую еду. Отсюда сам собой выходит
// сюжет, который нигде не прописан: голодающее поселение распродаёт запасы
// сырья, чтобы купить хлеб.
//
// Цена еды не постоянна, а следует за спросом: она считается по тому, на
// сколько лет вперёд миру хватит уже собранного зерна. В сытый год хлеб
// дёшев, в голодный дорожает вчетверо — и золото торговых поселений
// обесценивается ровно тогда, когда оно нужнее всего. Мировой цены на сырьё
// у модели по-прежнему нет — дефицита дерева или камня в целом мире не
// существует, и выдумывать его не из чего. Зато есть местная надбавка за
// качество: цех умелых мастеров одного материала продаёт дороже базовой
// цены (см. GuildSystem.GetQualityPremium) — та же земля и то же дерево,
// но чужаку не свезло родиться в городе ремесленных династий.
//
// Роскошь (см. MaterialType.Luxury, ремесло — Ювелир) — не сырьё вовсе:
// ни одна постройка её не потребляет, единственная судьба этого товара —
// уйти на внешний рынок по цене на порядок выше прочих. Ей же достаётся
// и надбавка гильдии — украшения мастера ценятся ровно тем же приёмом,
// что и кузнечная работа
//
// Сколько удастся сбыть — зависит от купцов: заезжий торговец подберёт
// немного в любом поселении, но всерьёз вывозит товар только тот, кто живёт
// этим ремеслом (категория Trade)
public static class MarketSystem
{
    private const double BaseFoodPrice = 0.5; // Золота за единицу еды в обычный год
    private const double ReferenceYearsOfSupply = 5; // Запас, при котором цена держится базовой

    private const double MinPriceFactor = 0.5; // В сытые годы хлеб дешевеет, но не даром
    private const double MaxPriceFactor = 4.0; // В голодные дорожает, но не бесконечно

    private const double MaterialPrice = 0.3;
    private const double LuxuryPrice = 2.4; // Роскошь — не сырьё для построек, ценится не по весу, а по редкости

    private const double SellRate = 0.5; // Купец берёт товар вдвое дешевле, чем продаёт — на разнице и живёт

    private const double BaseCarry = 5; // Заезжий торговец подберёт немного и там, где своих купцов нет
    private const double GoodsPerTrader = 20; // Сколько лишнего товара один свой купец успевает вывезти за год

    public static void Process(World world)
    {
        var foodPrice = GetFoodPrice(world);

        foreach (var settlement in world.Settlements)
        {
            SellSurplus(settlement, world, foodPrice);
            BuyFood(settlement, foodPrice);
        }
    }

    // Цена единицы еды в золоте: чем на меньшее число лет миру хватит запаса, тем дороже хлеб
    public static double GetFoodPrice(World world)
    {
        var demand = world.Characters.Count(c => c.Alive) * world.Settings.FoodConsumptionPerCapita;

        if (demand <= 0)
        {
            return BaseFoodPrice; // Покупателей не осталось — цену не на чем строить
        }

        var supply = world.Settlements.Sum(s => Math.Max(0, s.FoodStock));
        var yearsOfSupply = supply / demand;

        var factor = yearsOfSupply <= 0
            ? MaxPriceFactor
            : Math.Clamp(ReferenceYearsOfSupply / yearsOfSupply, MinPriceFactor, MaxPriceFactor);

        return BaseFoodPrice * factor;
    }

    // Сбыт излишка, которому не хватило места на складе: его всё равно ждала
    // порча, поэтому продают даже по половинной цене
    private static void SellSurplus(Settlement settlement, World world, double foodPrice)
    {
        var traders = settlement.Members.Count(m =>
            m.Alive && ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Trade);

        // Порт и пристань вывозят больше обычной подводы (см. TerrainSystem.Relief.Coast)
        var carryLeft = (BaseCarry + traders * GoodsPerTrader) * TerrainSystem.GetTradeCapacityMultiplier(settlement, world);

        var foodSurplus = settlement.FoodStock - StorageSystem.GetFoodCapacity(settlement);

        if (foodSurplus > 0)
        {
            var sold = Math.Min(foodSurplus, carryLeft);

            settlement.FoodStock -= sold;
            settlement.Gold += sold * foodPrice * SellRate;
            carryLeft -= sold;
        }

        foreach (var type in settlement.MaterialStocks.Keys.ToList())
        {
            if (carryLeft <= 0)
            {
                break;
            }

            var surplus = settlement.MaterialStocks[type] - StorageSystem.GetMaterialCapacity(settlement, type);

            if (surplus <= 0)
            {
                continue;
            }

            var sold = Math.Min(surplus, carryLeft);
            var price = type == MaterialType.Luxury ? LuxuryPrice : MaterialPrice;

            settlement.MaterialStocks[type] -= sold;
            settlement.Gold += sold * price * GuildSystem.GetQualityPremium(settlement, type, world) * SellRate;
            carryLeft -= sold;
        }
    }

    private static void BuyFood(Settlement settlement, double foodPrice)
    {
        if (settlement.FoodStock >= 0 || settlement.Gold <= 0 || foodPrice <= 0)
        {
            return;
        }

        var neededFood = -settlement.FoodStock;
        var affordableFood = settlement.Gold / foodPrice;
        var boughtFood = Math.Min(neededFood, affordableFood);

        settlement.Gold -= boughtFood * foodPrice;
        settlement.FoodStock += boughtFood;
    }
}
