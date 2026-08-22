using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Землетрясение и паводок — катастрофы, привязанные к рельефу (см. TerrainSystem):
// горы трясёт, низину и приморье топит, холмы — ни то ни другое. Проверяется
// само разделение по видам земли и то, что каждая катастрофа бьёт по своему —
// землетрясение по домам и стенам, паводок по запасам сырья
public class DisasterSystemTests
{
    private const int Seed = 42;

    private static (World World, Settlement Settlement) BuildSettlement(Relief relief)
    {
        var coordinate = FindCoordinate(relief);
        var world = new World
        {
            CurrentYear = 100,
            Seed = Seed,
            Settings = new WorldSettings { DisasterChance = 1.0 } // катастрофа гарантирована — случаен только вид
        };

        var settlement = new Settlement { Id = 1, Name = "Тестовое", X = coordinate.X, Y = coordinate.Y, Houses = 10, Walls = 10 };
        settlement.MaterialStocks[MaterialType.Wood] = 100;

        for (var i = 0; i < 50; i++)
        {
            var resident = new Character
            {
                Id = i + 1,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = "Фермер"
            };

            settlement.Members.Add(resident);
            world.Characters.Add(resident);
        }

        world.Settlements.Add(settlement);

        return (world, settlement);
    }

    [Fact]
    public void Process_MountainSettlement_EventuallyTriggersEarthquake()
    {
        var (world, _) = BuildSettlement(Relief.Mountain);

        Rng.Initialize(seed: 1);

        var triggered = false;

        for (var year = 0; year < 200 && !triggered; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
            triggered = world.Events.Any(e => e.Description.Contains("землетрясение"));
        }

        Assert.True(triggered, "горному поселению за 200 лет полагалось хотя бы одно землетрясение");
    }

    [Fact]
    public void Process_LowlandSettlement_EventuallyTriggersFlood()
    {
        var (world, _) = BuildSettlement(Relief.Lowland);

        Rng.Initialize(seed: 1);

        var triggered = false;

        for (var year = 0; year < 200 && !triggered; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
            triggered = world.Events.Any(e => e.Description.Contains("паводок"));
        }

        Assert.True(triggered, "низинному поселению за 200 лет полагался хотя бы один паводок");
    }

    [Fact]
    public void Process_CoastalSettlement_EventuallyTriggersFlood()
    {
        var (world, _) = BuildSettlement(Relief.Coast);

        Rng.Initialize(seed: 1);

        var triggered = false;

        for (var year = 0; year < 200 && !triggered; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
            triggered = world.Events.Any(e => e.Description.Contains("паводок"));
        }

        Assert.True(triggered, "приморскому поселению за 200 лет полагался хотя бы один паводок");
    }

    [Fact]
    public void Process_LowlandSettlement_NeverTriggersEarthquake()
    {
        var (world, _) = BuildSettlement(Relief.Lowland);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
        }

        Assert.DoesNotContain(world.Events, e => e.Description.Contains("землетрясение"));
    }

    [Fact]
    public void Process_MountainSettlement_NeverTriggersFlood()
    {
        var (world, _) = BuildSettlement(Relief.Mountain);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
        }

        Assert.DoesNotContain(world.Events, e => e.Description.Contains("паводок"));
    }

    [Fact]
    public void Process_HillSettlement_NeverTriggersEarthquakeOrFlood()
    {
        var (world, _) = BuildSettlement(Relief.Hill);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);
        }

        Assert.DoesNotContain(world.Events, e => e.Description.Contains("землетрясение") || e.Description.Contains("паводок"));
    }

    [Fact]
    public void Process_Earthquake_DestroysHousesOrWalls()
    {
        var (world, settlement) = BuildSettlement(Relief.Mountain);
        var housesBefore = settlement.Houses;
        var wallsBefore = settlement.Walls;

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);

            if (settlement.Houses < housesBefore || settlement.Walls < wallsBefore)
            {
                return; // нашли — землетрясение действительно рушит постройки
            }
        }

        Assert.Fail("за 200 лет землетрясение ни разу не тронуло дома или стены");
    }

    [Fact]
    public void Process_Flood_WashesAwayMaterialStocks()
    {
        var (world, settlement) = BuildSettlement(Relief.Coast);
        var stockBefore = settlement.MaterialStocks[MaterialType.Wood];

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            DisasterSystem.Process(world);

            if (settlement.MaterialStocks[MaterialType.Wood] < stockBefore)
            {
                return; // нашли — паводок действительно смывает запасы
            }
        }

        Assert.Fail("за 200 лет паводок ни разу не тронул запасы сырья");
    }

    // Тот же приём поиска координат по рельефу, что и в TerrainSystemTests, —
    // широта держится неизменной, чтобы плодородие климата не примешивалось
    private static (double X, double Y) FindCoordinate(Relief target)
    {
        const double y = ClimateSystem.MapSize / 2;

        for (double x = 0; x <= ClimateSystem.MapSize; x += 5)
        {
            if (TerrainSystem.GetRelief(x, y, Seed) == target)
            {
                return (x, y);
            }
        }

        throw new InvalidOperationException($"На широте {y} не нашлось точки рельефа {target} — тест нужно пересмотреть");
    }
}
