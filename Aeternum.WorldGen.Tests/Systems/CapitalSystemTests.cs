using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Столица и тирания расстояния. Проверяется, где встаёт престол, когда он
// переносится (и когда не должен), и как удалённость земли сказывается
// на дани и на терпении её жителей
public class CapitalSystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Home) BuildKingdom()
    {
        var world = new World { CurrentYear = 100 };
        var home = new Settlement { Id = 1, Name = "Родной город", X = 0, Y = 0 };

        var ruler = Add(world, home, "Правитель");

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [home],
            TributeRate = world.Settings.TributeRate
        };

        world.Kingdoms.Add(kingdom);

        return (world, kingdom, home);
    }

    private static Character Add(World world, Settlement settlement, string name = "Житель")
    {
        var character = new Character
        {
            Id = world.Characters.Count + 1,
            Name = $"{name}{world.Characters.Count}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = "Фермер"
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        if (!world.Settlements.Contains(settlement))
        {
            world.Settlements.Add(settlement);
        }

        return character;
    }

    [Fact]
    public void Process_NewKingdom_SeatsItselfInTheRulersTown()
    {
        var (world, kingdom, home) = BuildKingdom();

        CapitalSystem.Process(world);

        Assert.Equal(home, kingdom.Capital);
    }

    [Fact]
    public void Process_NewRulerFromAnotherTown_DoesNotMoveTheThrone()
    {
        // Престол — институция, а не адрес нынешнего государя
        var (world, kingdom, home) = BuildKingdom();
        CapitalSystem.Process(world);

        var elsewhere = new Settlement { Id = 2, Name = "Другой город", X = 500, Y = 500 };
        var heir = Add(world, elsewhere);

        kingdom.Settlements.Add(elsewhere);
        kingdom.Ruler = heir;

        CapitalSystem.Process(world);

        Assert.Equal(home, kingdom.Capital);
        Assert.DoesNotContain(world.Events, e => e.Type == EventType.CapitalMoved);
    }

    [Fact]
    public void Process_LostCapital_MovesTheThrone()
    {
        var (world, kingdom, home) = BuildKingdom();
        CapitalSystem.Process(world);

        var refuge = new Settlement { Id = 2, Name = "Убежище", X = 500, Y = 500 };
        Add(world, refuge);

        kingdom.Settlements.Remove(home); // Столица отпала от государства
        kingdom.Settlements.Add(refuge);

        CapitalSystem.Process(world);

        Assert.Equal(refuge, kingdom.Capital);
        Assert.Contains(world.Events, e => e.Type == EventType.CapitalMoved);
    }

    [Fact]
    public void Process_DeadCapital_MovesTheThrone()
    {
        var (world, kingdom, home) = BuildKingdom();
        CapitalSystem.Process(world);

        var living = new Settlement { Id = 2, Name = "Живой город", X = 100, Y = 100 };
        Add(world, living);
        kingdom.Settlements.Add(living);

        foreach (var resident in home.Members)
        {
            resident.Alive = false;
        }

        CapitalSystem.Process(world);

        Assert.Equal(living, kingdom.Capital);
    }

    [Fact]
    public void GetControl_FallsWithDistance()
    {
        var (world, kingdom, home) = BuildKingdom();
        CapitalSystem.Process(world);

        var near = new Settlement { Id = 2, Name = "Ближняя", X = 50, Y = 0 };
        var far = new Settlement { Id = 3, Name = "Дальняя", X = 900, Y = 0 };

        Assert.Equal(1.0, CapitalSystem.GetControl(kingdom, home));
        Assert.True(CapitalSystem.GetControl(kingdom, near) > CapitalSystem.GetControl(kingdom, far));
        Assert.True(CapitalSystem.GetControl(kingdom, far) > 0, "даже дальняя окраина остаётся частью государства");
    }

    [Fact]
    public void GetControl_WithoutCapital_IsFull()
    {
        // Пока престола нет, расстоянию не от чего отсчитываться
        var (world, kingdom, home) = BuildKingdom();

        Assert.Equal(1.0, CapitalSystem.GetControl(kingdom, home));
    }

    [Fact]
    public void TributeProcess_DistantProvincePaysLess()
    {
        // Две одинаковые провинции, разница только в удалённости от престола
        var (world, kingdom, home) = BuildKingdom();

        var near = new Settlement { Id = 2, Name = "Ближняя", X = 30, Y = 0, FoodStock = 1000 };
        var far = new Settlement { Id = 3, Name = "Дальняя", X = 900, Y = 0, FoodStock = 1000 };

        Add(world, near);
        Add(world, far);

        kingdom.Settlements.Add(near);
        kingdom.Settlements.Add(far);

        CapitalSystem.Process(world);
        TributeSystem.Process(world);

        Assert.True(near.FoodStock < far.FoodStock, "с дальней земли корона собирает меньше");
    }

    [Fact]
    public void RebellionProcess_DistantProvinceRisesSooner()
    {
        // Тот же порядок, та же ставка — разница только в расстоянии
        var nearYears = YearsUntilRebellion(x: 30);
        var farYears = YearsUntilRebellion(x: 950);

        Assert.True(farYears < nearYears, $"дальняя окраина должна бунтовать раньше: {farYears} против {nearYears}");
    }

    private static int YearsUntilRebellion(double x)
    {
        var (world, kingdom, home) = BuildKingdom();

        var province = new Settlement { Id = 2, Name = "Провинция", X = x, Y = 0, FoodStock = 100 };
        Add(world, province);
        kingdom.Settlements.Add(province);

        CapitalSystem.Process(world);

        Rng.Initialize(seed: 1);

        for (var year = 1; year <= 2000; year++)
        {
            world.CurrentYear = 100 + year;
            kingdom.FoodTreasury = 0; // Подавить корона не может — смотрим именно на мятеж

            RebellionSystem.Process(world);

            if (RebellionSystem.IsRebelling(province, world))
            {
                return year;
            }
        }

        return int.MaxValue;
    }
}
