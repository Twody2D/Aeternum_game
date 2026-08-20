using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Налоговая политика — единственное место, где правитель принимает решение,
// а не подчиняется мировой константе. Проверяется и сам выбор ставки, и то,
// что собранное возвращается землям
public class TributeSystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Province) BuildKingdom(int provincePopulation = 10)
    {
        var world = new World();
        var capital = new Settlement { Id = 1, Name = "Столица" };
        var province = new Settlement { Id = 2, Name = "Провинция" };

        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true, Settlement = capital };
        capital.Members.Add(ruler);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [capital, province],
            TributeRate = world.Settings.TributeRate
        };

        for (var i = 0; i < provincePopulation; i++)
        {
            var resident = new Character
            {
                Id = 100 + i,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = province
            };

            province.Members.Add(resident);
            world.Characters.Add(resident);
        }

        world.Characters.Add(ruler);
        world.Settlements.Add(capital);
        world.Settlements.Add(province);
        world.Kingdoms.Add(kingdom);

        return (world, kingdom, province);
    }

    [Fact]
    public void ChooseRate_EmptyTreasury_RaisesTheRate()
    {
        var (world, kingdom, _) = BuildKingdom();
        kingdom.FoodTreasury = 0;

        Assert.True(TributeSystem.ChooseRate(kingdom, world) > world.Settings.TributeRate);
    }

    [Fact]
    public void ChooseRate_FullTreasury_LowersTheRate()
    {
        // Богатой короне незачем душить землю поборами
        var (world, kingdom, _) = BuildKingdom();
        kingdom.FoodTreasury = StorageSystem.GetTreasuryCapacity(kingdom);

        Assert.True(TributeSystem.ChooseRate(kingdom, world) < world.Settings.TributeRate);
    }

    [Fact]
    public void ChooseRate_GoldCountsAsWealthToo()
    {
        // Богатство короны меряется тем же, чем устойчивость трона: зерном и золотом
        var (world, kingdom, _) = BuildKingdom();
        var poor = TributeSystem.ChooseRate(kingdom, world);

        kingdom.GoldTreasury = StorageSystem.GetTreasuryCapacity(kingdom) * 2;

        Assert.True(TributeSystem.ChooseRate(kingdom, world) < poor);
    }

    [Fact]
    public void ChooseRate_SiegeInOwnLands_RaisesTheRate()
    {
        var (world, kingdom, province) = BuildKingdom();
        kingdom.FoodTreasury = StorageSystem.GetTreasuryCapacity(kingdom); // Богатую корону поднимает вверх только беда
        var peaceful = TributeSystem.ChooseRate(kingdom, world);

        province.SiegeYears = 2;

        Assert.True(TributeSystem.ChooseRate(kingdom, world) > peaceful);
    }

    [Fact]
    public void ChooseRate_RebellionInOwnLands_RaisesTheRate()
    {
        // Мятежное поселение выпадает из земель короны, но денег требует именно оно
        var (world, kingdom, province) = BuildKingdom();
        kingdom.FoodTreasury = StorageSystem.GetTreasuryCapacity(kingdom);
        var quiet = TributeSystem.ChooseRate(kingdom, world);

        province.RebellingAgainst = kingdom;
        province.RebellingUntilYear = world.CurrentYear + 5;

        Assert.True(TributeSystem.ChooseRate(kingdom, world) > quiet);
    }

    [Fact]
    public void ChooseRate_BoldRulerTakesMoreThanPrudentOne()
    {
        var (world, kingdom, _) = BuildKingdom();

        kingdom.Ruler.Traits.Add(Trait.Brave);
        var bold = TributeSystem.ChooseRate(kingdom, world);

        kingdom.Ruler.Traits.Clear();
        kingdom.Ruler.Traits.Add(Trait.Prudent);
        var prudent = TributeSystem.ChooseRate(kingdom, world);

        Assert.True(bold > prudent);
    }

    [Fact]
    public void ChooseRate_StaysWithinSaneBounds()
    {
        // Ни дани без остатка, ни государства вовсе без дани
        var (world, kingdom, province) = BuildKingdom();

        kingdom.FoodTreasury = 0;
        province.SiegeYears = 10;
        kingdom.Ruler.Traits.Add(Trait.Brave);

        var greediest = TributeSystem.ChooseRate(kingdom, world);

        kingdom.FoodTreasury = StorageSystem.GetTreasuryCapacity(kingdom) * 10;
        province.SiegeYears = 0;
        kingdom.Ruler.Traits.Clear();
        kingdom.Ruler.Traits.Add(Trait.Prudent);

        var mildest = TributeSystem.ChooseRate(kingdom, world);

        Assert.True(greediest <= 0.5, $"даже жадный правитель не забирает всё: {greediest:P0}");
        Assert.True(mildest > 0, "совсем без дани не обходится никто");
        Assert.True(greediest > mildest);
    }

    [Fact]
    public void Process_CollectsAtTheKingdomsOwnRate()
    {
        // Мировая настройка теперь лишь точка отсчёта, а берут по своей ставке
        var (world, kingdom, province) = BuildKingdom();
        province.FoodStock = 1000;
        kingdom.TributeRate = 0.25;

        TributeSystem.Process(world);

        Assert.True(kingdom.FoodTreasury > 1000 * world.Settings.TributeRate,
            "собрано должно быть по ставке короны, а не по общемировой");
    }

    [Fact]
    public void Process_StarvingProvince_IsFedFromTreasury()
    {
        var (world, kingdom, province) = BuildKingdom();
        province.FoodStock = -50;
        kingdom.FoodTreasury = 1000;

        TributeSystem.Process(world);

        Assert.True(province.FoodStock > -50, "корона обязана делиться запасом со своей голодающей землёй");
        Assert.True(kingdom.FoodTreasury < 1000);
    }

    [Fact]
    public void Process_ReliefNeverEmptiesTheTreasury()
    {
        // Оставшееся нужно короне на походы и на следующий год
        var (world, kingdom, province) = BuildKingdom(provincePopulation: 200);
        province.FoodStock = -10_000;
        kingdom.FoodTreasury = 1000;

        TributeSystem.Process(world);

        Assert.True(kingdom.FoodTreasury > 0, "казна не должна опустошаться раздачей досуха");
    }

    [Fact]
    public void Process_RebellingProvince_GetsNothing()
    {
        var (world, kingdom, province) = BuildKingdom();
        province.FoodStock = -50;
        province.RebellingAgainst = kingdom;
        province.RebellingUntilYear = world.CurrentYear + 5;
        kingdom.FoodTreasury = 1000;

        TributeSystem.Process(world);

        Assert.Equal(-50, province.FoodStock);
    }

    [Fact]
    public void Process_EmptySettlement_IsNotFed()
    {
        // Кормить некого — запас пустого поселения короне тратить незачем
        var (world, kingdom, _) = BuildKingdom(provincePopulation: 0);
        var capital = kingdom.Settlements[0];

        kingdom.Settlements.Remove(capital); // Оставляем только пустую провинцию
        kingdom.FoodTreasury = 1000;

        TributeSystem.Process(world);

        Assert.Equal(1000, kingdom.FoodTreasury);
    }
}
