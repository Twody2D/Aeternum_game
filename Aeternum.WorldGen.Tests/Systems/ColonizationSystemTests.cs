using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Обычная колония садится рядом с родителем (см. SettlementGenerator.
// ColonyOffsetRange = 50). Прибрежное (TerrainSystem.Relief.Coast) и достаточно
// разбогатевшее на торговле поселение может вместо этого снарядить колонию
// за море — куда угодно на карте, без ограничения по расстоянию
public class ColonizationSystemTests
{
    private const int Seed = 42;

    private static (World World, Settlement Origin) BuildReadyToColonize(Relief relief, double gold)
    {
        var coordinate = FindCoordinate(relief);
        var world = new World { CurrentYear = 100, Seed = Seed };

        var origin = new Settlement
        {
            Id = 1, Name = "Метрополия", X = coordinate.X, Y = coordinate.Y,
            Gold = gold
        };
        origin.MaterialStocks[MaterialType.Wood] = 1000; // С запасом выше ColonizationMaterialCost

        var father = new Character { Id = 1, Name = "Отец", LastName = "Тестов", Age = 35, Alive = true, LifeStage = LifeStage.Adult, Settlement = origin };
        var mother = new Character { Id = 2, Name = "Мать", LastName = "Тестова", Age = 33, Alive = true, LifeStage = LifeStage.Adult, Settlement = origin };
        var family = new Family { Id = 1, Father = father, Mother = mother, FormedYear = 90 };

        father.CurrentFamily = family;
        mother.CurrentFamily = family;
        origin.Members.AddRange([father, mother]);
        world.Characters.AddRange([father, mother]);
        world.Families.Add(family);

        // Остальное население — просто числом, для порога ColonizationPopulationThreshold
        for (var i = 0; i < 30; i++)
        {
            var resident = new Character { Id = 10 + i, Name = $"Житель{i}", LastName = "Тестов", Age = 30, Alive = true, LifeStage = LifeStage.Adult, Settlement = origin };
            origin.Members.Add(resident);
            world.Characters.Add(resident);
        }

        world.Settlements.Add(origin);

        return (world, origin);
    }

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

    private static double Distance(Settlement a, Settlement b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    [Fact]
    public void Process_CoastalWealthySettlement_SometimesFoundsColonyFarBeyondTheUsualRange()
    {
        var foundedFar = false;

        for (var run = 0; run < 500 && !foundedFar; run++)
        {
            var (world, origin) = BuildReadyToColonize(Relief.Coast, gold: 500);

            Rng.Initialize(seed: run + 1);
            ColonizationSystem.Process(world);

            var founded = world.Settlements.FirstOrDefault(s => s != origin);
            foundedFar = founded != null && Distance(origin, founded) > 150;
        }

        Assert.True(foundedFar, "хотя бы раз за 500 попыток богатое приморье должно основать колонию далеко за обычным радиусом");
    }

    [Fact]
    public void Process_InlandWealthySettlement_NeverFoundsColonyFarBeyondTheUsualRange()
    {
        for (var run = 0; run < 300; run++)
        {
            var (world, origin) = BuildReadyToColonize(Relief.Lowland, gold: 500);

            Rng.Initialize(seed: run + 1);
            ColonizationSystem.Process(world);

            var founded = world.Settlements.FirstOrDefault(s => s != origin);

            if (founded != null)
            {
                Assert.True(Distance(origin, founded) <= 100, "без выхода к морю колония не должна оказаться дальше обычного радиуса (с запасом на диагональ ColonyOffsetRange)");
            }
        }
    }

    [Fact]
    public void Process_CoastalButPoorSettlement_NeverFoundsColonyFarBeyondTheUsualRange()
    {
        for (var run = 0; run < 300; run++)
        {
            var (world, origin) = BuildReadyToColonize(Relief.Coast, gold: 0); // Ниже MinGoldForOverseasColony

            Rng.Initialize(seed: run + 1);
            ColonizationSystem.Process(world);

            var founded = world.Settlements.FirstOrDefault(s => s != origin);

            if (founded != null)
            {
                Assert.True(Distance(origin, founded) <= 100, "без торгового богатства даже приморье не должно основать колонию дальше обычного радиуса (с запасом на диагональ ColonyOffsetRange)");
            }
        }
    }
}
