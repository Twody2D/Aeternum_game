using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Цена еды — единственное число в мире, которое считается по состоянию всего
// мира сразу, а не по одному поселению. Проверяется и она, и обе стороны
// торга, которые от неё зависят
public class MarketSystemTests
{
    private static Character Resident(int id, Settlement settlement, string profession)
    {
        return new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = profession
        };
    }

    private static World WorldWith(Settlement settlement, int population, string profession = "Фермер")
    {
        var world = new World();
        world.Settlements.Add(settlement);

        for (var i = 0; i < population; i++)
        {
            var resident = Resident(i + 1, settlement, profession);
            settlement.Members.Add(resident);
            world.Characters.Add(resident);
        }

        return world;
    }

    [Fact]
    public void GetFoodPrice_HungryWorld_CostsMoreThanWellFedOne()
    {
        var lean = new Settlement { Id = 1, Name = "Голодная", FoodStock = 5 };
        var leanWorld = WorldWith(lean, population: 20);

        var fat = new Settlement { Id = 1, Name = "Сытая", FoodStock = 5000 };
        var fatWorld = WorldWith(fat, population: 20);

        Assert.True(MarketSystem.GetFoodPrice(leanWorld) > MarketSystem.GetFoodPrice(fatWorld));
    }

    [Fact]
    public void GetFoodPrice_StaysWithinBounds()
    {
        // Ни бесплатного хлеба в урожайный век, ни бесконечной цены в голодный
        var starving = new Settlement { Id = 1, Name = "Пустая", FoodStock = 0 };
        var starvingWorld = WorldWith(starving, population: 50);

        var glutted = new Settlement { Id = 1, Name = "Полная", FoodStock = 1_000_000 };
        var gluttedWorld = WorldWith(glutted, population: 1);

        var high = MarketSystem.GetFoodPrice(starvingWorld);
        var low = MarketSystem.GetFoodPrice(gluttedWorld);

        Assert.True(low > 0, "хлеб не бывает даровым");
        Assert.True(high / low <= 10, $"разброс цены должен быть ограничен, а он {high / low:0.#}");
    }

    [Fact]
    public void GetFoodPrice_EmptyWorld_HasBasePrice()
    {
        // Покупателей не осталось — цену не на чем строить, но и делить на ноль нельзя
        var world = new World();

        Assert.True(MarketSystem.GetFoodPrice(world) > 0);
    }

    [Fact]
    public void Process_SurplusOverCapacity_IsSoldForGold()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = WorldWith(settlement, population: 5);

        settlement.MaterialStocks[MaterialType.Wood] = StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood) + 100;

        MarketSystem.Process(world);

        Assert.True(settlement.Gold > 0, "лишнее сырьё обязано превращаться в золото");
        Assert.True(settlement.MaterialStocks[MaterialType.Wood] < StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood) + 100);
    }

    [Fact]
    public void Process_StockWithinCapacity_IsNotSold()
    {
        // Продают только то, что всё равно не пережило бы года на складе
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = WorldWith(settlement, population: 5);
        var capacity = StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood);

        settlement.MaterialStocks[MaterialType.Wood] = capacity;

        MarketSystem.Process(world);

        Assert.Equal(capacity, settlement.MaterialStocks[MaterialType.Wood]);
        Assert.Equal(0, settlement.Gold);
    }

    [Fact]
    public void Process_TradersCarryMoreThanPassingMerchants()
    {
        // Своё купечество — не украшение: с ним поселение сбывает заметно больше
        var withoutTraders = new Settlement { Id = 1, Name = "Без купцов" };
        var plainWorld = WorldWith(withoutTraders, population: 5);

        var withTraders = new Settlement { Id = 1, Name = "С купцами" };
        var traderWorld = WorldWith(withTraders, population: 5, profession: "Торговец");

        foreach (var settlement in new[] { withoutTraders, withTraders })
        {
            settlement.MaterialStocks[MaterialType.Wood] =
                StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood) + 1000;
        }

        MarketSystem.Process(plainWorld);
        MarketSystem.Process(traderWorld);

        Assert.True(withTraders.Gold > withoutTraders.Gold);
        Assert.True(withoutTraders.Gold > 0, "мимо проезжают и там, где своих купцов нет");
    }

    [Fact]
    public void Process_HungrySettlementWithGold_BuysFood()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка", FoodStock = -20, Gold = 100 };
        var world = WorldWith(settlement, population: 5);

        MarketSystem.Process(world);

        Assert.True(settlement.FoodStock > -20, "золото обязано превращаться в хлеб");
        Assert.True(settlement.Gold < 100);
    }

    [Fact]
    public void Process_HungrySettlementWithoutGold_StaysHungry()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка", FoodStock = -20, Gold = 0 };
        var world = WorldWith(settlement, population: 5);

        MarketSystem.Process(world);

        Assert.Equal(-20, settlement.FoodStock);
    }

    [Fact]
    public void Process_BuyingNeverOvershootsTheDeficit()
    {
        // Впрок на внешнем рынке не закупаются: только закрыть дыру
        var settlement = new Settlement { Id = 1, Name = "Тестовка", FoodStock = -10, Gold = 1_000_000 };
        var world = WorldWith(settlement, population: 5);

        MarketSystem.Process(world);

        Assert.Equal(0, settlement.FoodStock, precision: 6);
    }

    [Fact]
    public void Process_SellingThenBuying_LetsHungrySettlementTradeItsWayOut()
    {
        // Ради этой связки рынок и стоит после строек: голодающее поселение
        // сбывает лишнее сырьё и на выручку берёт хлеб — в один и тот же год
        var settlement = new Settlement { Id = 1, Name = "Тестовка", FoodStock = -5, Gold = 0 };
        var world = WorldWith(settlement, population: 5, profession: "Торговец");

        settlement.MaterialStocks[MaterialType.Clay] = StorageSystem.GetMaterialCapacity(settlement, MaterialType.Clay) + 100;

        MarketSystem.Process(world);

        Assert.True(settlement.FoodStock > -5, "выручка того же года обязана идти в дело");
    }

    [Fact]
    public void Process_Luxury_SellsForMoreThanOrdinaryMaterial()
    {
        // Роскошь — не сырьё для построек, ценится по редкости, а не по весу
        var ordinary = new Settlement { Id = 1, Name = "Ремесленная" };
        var ordinaryWorld = WorldWith(ordinary, population: 5);
        ordinary.MaterialStocks[MaterialType.Clay] = StorageSystem.GetMaterialCapacity(ordinary, MaterialType.Clay) + 100;

        var luxurious = new Settlement { Id = 1, Name = "Ювелирная" };
        var luxuriousWorld = WorldWith(luxurious, population: 5);
        luxurious.MaterialStocks[MaterialType.Luxury] = StorageSystem.GetMaterialCapacity(luxurious, MaterialType.Luxury) + 100;

        MarketSystem.Process(ordinaryWorld);
        MarketSystem.Process(luxuriousWorld);

        Assert.True(luxurious.Gold > ordinary.Gold,
            $"та же сотня единиц товара сверх склада должна давать больше золота в роскоши: {luxurious.Gold} против {ordinary.Gold}");
    }
}
