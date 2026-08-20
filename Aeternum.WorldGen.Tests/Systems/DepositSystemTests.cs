using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Сырьё берётся не только из профессии, но и из земли под ногами (см. TerrainSystem):
// камень и металл — из гор, дерево и глина — из низин. Проверяется сам множитель
// и то, что хозяйство действительно добывает по-разному в разных краях
public class DepositSystemTests
{
    private const int Seed = 42;

    [Fact]
    public void GetYieldMultiplier_OreFavoursRuggedTerrain()
    {
        var lowland = BuildSettlement(FindCoordinate(Relief.Lowland));
        var mountain = BuildSettlement(FindCoordinate(Relief.Mountain));
        var world = new World { Seed = Seed };

        Assert.True(DepositSystem.GetYieldMultiplier(mountain, MaterialType.Stone, world)
                    > DepositSystem.GetYieldMultiplier(lowland, MaterialType.Stone, world));
        Assert.True(DepositSystem.GetYieldMultiplier(mountain, MaterialType.Metal, world)
                    > DepositSystem.GetYieldMultiplier(lowland, MaterialType.Metal, world));
    }

    [Fact]
    public void GetYieldMultiplier_TimberAndClayFavourLowland()
    {
        var lowland = BuildSettlement(FindCoordinate(Relief.Lowland));
        var mountain = BuildSettlement(FindCoordinate(Relief.Mountain));
        var world = new World { Seed = Seed };

        Assert.True(DepositSystem.GetYieldMultiplier(lowland, MaterialType.Wood, world)
                    > DepositSystem.GetYieldMultiplier(mountain, MaterialType.Wood, world));
        Assert.True(DepositSystem.GetYieldMultiplier(lowland, MaterialType.Clay, world)
                    > DepositSystem.GetYieldMultiplier(mountain, MaterialType.Clay, world));
    }

    [Fact]
    public void GetYieldMultiplier_TextileIsNotTiedToRelief()
    {
        // Сырьё для тканей (лён, шерсть) — ремесло, а не геология: рельеф его не касается
        var lowland = BuildSettlement(FindCoordinate(Relief.Lowland));
        var mountain = BuildSettlement(FindCoordinate(Relief.Mountain));
        var world = new World { Seed = Seed };

        Assert.Equal(1.0, DepositSystem.GetYieldMultiplier(lowland, MaterialType.Textile, world));
        Assert.Equal(1.0, DepositSystem.GetYieldMultiplier(mountain, MaterialType.Textile, world));
    }

    [Fact]
    public void EconomyProcess_MountainSettlement_MinesMoreMetalThanLowland()
    {
        // Проверяется не сама шкала месторождения, а то, что хозяйство её слушает
        var lowlandMetal = MetalProducedAt(FindCoordinate(Relief.Lowland));
        var mountainMetal = MetalProducedAt(FindCoordinate(Relief.Mountain));

        Assert.True(mountainMetal > lowlandMetal, $"горы должны давать больше металла, чем низина: {mountainMetal} против {lowlandMetal}");
    }

    private static double MetalProducedAt((double X, double Y) coordinate)
    {
        var world = new World { CurrentYear = 1, Seed = Seed };
        var settlement = BuildSettlement(coordinate);

        for (var i = 0; i < 5; i++)
        {
            var smith = new Character
            {
                Id = i + 1,
                Name = $"Кузнец{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = "Кузнец"
            };

            settlement.Members.Add(smith);
            world.Characters.Add(smith);
        }

        world.Settlements.Add(settlement);

        EconomySystem.Process(world);

        return settlement.MaterialStocks.GetValueOrDefault(MaterialType.Metal);
    }

    private static Settlement BuildSettlement((double X, double Y) coordinate)
    {
        return new Settlement { Id = 1, Name = "Тестовое", X = coordinate.X, Y = coordinate.Y };
    }

    // Тот же приём поиска координат по рельефу, что и в TerrainSystemTests, —
    // широта держится неизменной, чтобы плодородие климата не примешивалось
    private static (double X, double Y) FindCoordinate(Relief target)
    {
        const double y = ClimateSystem.MapSize / 2;

        for (double x = 0; x <= ClimateSystem.MapSize; x += 5)
        {
            if (TerrainSystem.GetRelief(TerrainSystem.GetElevation(x, y, Seed)) == target)
            {
                return (x, y);
            }
        }

        throw new InvalidOperationException($"На широте {y} не нашлось точки рельефа {target} — тест нужно пересмотреть");
    }
}
