using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Хранение — единственное место, где запас убывает сам по себе, без чьего-либо
// решения. Проверяются оба правила разом (порча и переполнение) и граница между
// ними: гниёт только еда, а дефицит не трогается вовсе
public class StorageSystemTests
{
    private static World WorldWith(Settlement settlement)
    {
        var world = new World();
        world.Settlements.Add(settlement);

        return world;
    }

    [Fact]
    public void Process_FoodWithinCapacity_OnlySpoils()
    {
        // Даже полный амбар не хранит зерно вечно
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Houses = 4, FoodStock = 100 };
        var world = WorldWith(settlement);

        StorageSystem.Process(world);

        Assert.Equal(100 * (1 - world.Settings.FoodSpoilageRate), settlement.FoodStock, precision: 6);
    }

    [Fact]
    public void Process_FoodOverCapacity_LosesPartOfExcess()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Houses = 0, FoodStock = 1000 };
        var world = WorldWith(settlement);

        StorageSystem.Process(world);

        var capacity = StorageSystem.GetFoodCapacity(settlement);

        Assert.True(settlement.FoodStock > capacity, "излишек пропадает не мгновенно");
        Assert.True(settlement.FoodStock < 1000 * (1 - world.Settings.FoodSpoilageRate), "излишек сверх склада обязан убывать быстрее обычной порчи");
    }

    [Fact]
    public void Process_NegativeFoodStock_IsLeftAlone()
    {
        // Дефицит — это долг перед жителями (им занимается EconomySystem),
        // а гнить в пустом амбаре нечему
        var settlement = new Settlement { Id = 1, Name = "Тестовка", FoodStock = -50 };
        var world = WorldWith(settlement);

        StorageSystem.Process(world);

        Assert.Equal(-50, settlement.FoodStock);
    }

    [Fact]
    public void Process_MaterialsWithinCapacity_AreUnchanged()
    {
        // Камень не гниёт: у материалов есть только предел склада, но не порча
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        settlement.MaterialStocks[MaterialType.Stone] = 50;

        var world = WorldWith(settlement);

        StorageSystem.Process(world);

        Assert.Equal(50, settlement.MaterialStocks[MaterialType.Stone]);
    }

    [Fact]
    public void Process_MaterialsOverCapacity_LoseHalfOfExcess()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = WorldWith(settlement);
        var capacity = StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood);

        settlement.MaterialStocks[MaterialType.Wood] = capacity + 100;

        StorageSystem.Process(world);

        Assert.Equal(capacity + 100 * (1 - world.Settings.StorageOverflowLossRate), settlement.MaterialStocks[MaterialType.Wood], precision: 6);
    }

    [Fact]
    public void Process_RepeatedYears_StockStopsGrowingNearCapacity()
    {
        // Смысл всей системы: запас, который никто не тратит, сходится к
        // вместимости склада, а не растёт до бесконечности
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Houses = 2 };
        var world = WorldWith(settlement);
        var capacity = StorageSystem.GetFoodCapacity(settlement);

        for (var year = 0; year < 200; year++)
        {
            settlement.FoodStock += 30; // Устойчивый излишек год за годом
            StorageSystem.Process(world);
        }

        Assert.True(settlement.FoodStock < capacity * 2, $"запас должен упереться в склад, а он {settlement.FoodStock:0}");
    }

    [Fact]
    public void GetFoodCapacity_GrowsWithHouses()
    {
        var empty = new Settlement { Id = 1, Name = "Пустошь" };
        var built = new Settlement { Id = 2, Name = "Тестовка", Houses = 5 };

        Assert.True(StorageSystem.GetFoodCapacity(built) > StorageSystem.GetFoodCapacity(empty));
        Assert.True(StorageSystem.GetFoodCapacity(empty) > 0, "без построек хранить негде, но яма для зерна есть везде");
    }

    [Fact]
    public void GetMaterialCapacity_WorkshopExtendsOnlyItsOwnMaterial()
    {
        // Кузница расширяет склад металла, но не склад тканей
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        settlement.Workshops[MaterialType.Metal] = 3;

        Assert.True(StorageSystem.GetMaterialCapacity(settlement, MaterialType.Metal)
                    > StorageSystem.GetMaterialCapacity(settlement, MaterialType.Textile));
    }

    [Fact]
    public void GetMaterialCapacity_CoversColonizationCost()
    {
        // Иначе поселению было бы физически не накопить на колонию — самую
        // дорогую единичную трату в мире (см. ColonizationSystem)
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World();

        Assert.True(StorageSystem.GetMaterialCapacity(settlement, MaterialType.Wood) >= world.Settings.ColonizationMaterialCost);
    }

    [Fact]
    public void Process_TreasuryOverCapacity_IsTrimmed()
    {
        var world = new World();
        var settlement = new Settlement { Id = 1, Name = "Столица" };
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [settlement],
            FoodTreasury = 100_000
        };

        kingdom.MaterialTreasury[MaterialType.Wood] = 100_000;

        world.Settlements.Add(settlement);
        world.Kingdoms.Add(kingdom);

        // За один год пропадает лишь часть излишка — смотрим, к чему казна приходит
        for (var year = 0; year < 30; year++)
        {
            StorageSystem.Process(world);
        }

        var capacity = StorageSystem.GetTreasuryCapacity(kingdom);

        Assert.True(capacity > 0);
        Assert.True(kingdom.MaterialTreasury[MaterialType.Wood] < capacity * 1.1, "казна одного города не удержит стотысячный запас");
        Assert.True(kingdom.FoodTreasury < capacity, "казённое зерно вдобавок гниёт");
    }

    [Fact]
    public void GetTreasuryCapacity_ShrinksWithLostSettlements()
    {
        // Казну хранят по городам короны: потеряв земли, государство теряет
        // и способность запасать
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [new Settlement { Id = 1, Name = "Столица" }, new Settlement { Id = 2, Name = "Провинция" }]
        };

        var wide = StorageSystem.GetTreasuryCapacity(kingdom);

        kingdom.Settlements.RemoveAt(1);

        Assert.True(StorageSystem.GetTreasuryCapacity(kingdom) < wide);
    }

    [Fact]
    public void Process_Gold_IsNeverLost()
    {
        // Золото — единственное, что не гниёт и не требует амбара: на этом
        // держится вся польза торговых профессий (см. MarketSystem)
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Gold = 100_000 };
        var world = WorldWith(settlement);

        for (var year = 0; year < 50; year++)
        {
            StorageSystem.Process(world);
        }

        Assert.Equal(100_000, settlement.Gold);
    }
}
