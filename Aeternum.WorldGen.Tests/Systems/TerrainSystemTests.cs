using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Рельеф выводится из координат и зерна мира, как и климат, — ничего не хранится.
// Проверяется само деление местности на три вида, их доля по карте и обе цены
// рельефа: скупая земля в горах и трудность их взятия силой
public class TerrainSystemTests
{
    private const int Seed = 42;

    [Fact]
    public void GetElevation_StaysWithinBounds()
    {
        for (double x = 0; x <= ClimateSystem.MapSize; x += 37)
        {
            for (double y = 0; y <= ClimateSystem.MapSize; y += 37)
            {
                var elevation = TerrainSystem.GetElevation(x, y, Seed);
                Assert.True(elevation is >= 0 and <= 1, $"высота на ({x},{y}) вышла за границы: {elevation}");
            }
        }
    }

    [Fact]
    public void GetRelief_RespectsThresholds()
    {
        Assert.Equal(Relief.Lowland, TerrainSystem.GetRelief(0.4));
        Assert.Equal(Relief.Hill, TerrainSystem.GetRelief(0.400001));
        Assert.Equal(Relief.Hill, TerrainSystem.GetRelief(0.599999));
        Assert.Equal(Relief.Mountain, TerrainSystem.GetRelief(0.6));
    }

    [Fact]
    public void GetRelief_AllThreeKindsOccur_InRoughlyMeasuredProportions()
    {
        // Пороги 0.4/0.6 подобраны по замеру на 20000 точках карты: около 30%
        // низин, 40% холмов, 30% гор. Здесь — тот же замер меньшей выборкой,
        // чтобы порог не оказался мёртвым (не делил бы карту на практике)
        var counts = new Dictionary<Relief, int> { [Relief.Lowland] = 0, [Relief.Hill] = 0, [Relief.Mountain] = 0 };
        var rng = new Random(1);

        for (var i = 0; i < 3000; i++)
        {
            var x = rng.NextDouble() * ClimateSystem.MapSize;
            var y = rng.NextDouble() * ClimateSystem.MapSize;
            counts[TerrainSystem.GetRelief(TerrainSystem.GetElevation(x, y, Seed))]++;
        }

        foreach (var (relief, count) in counts)
        {
            var share = count / 3000.0;
            Assert.True(share is > 0.15 and < 0.55, $"{relief} занял {share:P0} карты — рельеф не должен вырождаться в один вид");
        }
    }

    [Fact]
    public void GetFertilityModifier_MountainsAreScarcerThanLowlands()
    {
        var lowland = BuildSettlement(FindCoordinate(Relief.Lowland));
        var hill = BuildSettlement(FindCoordinate(Relief.Hill));
        var mountain = BuildSettlement(FindCoordinate(Relief.Mountain));
        var world = new World { Seed = Seed };

        var lowlandFactor = TerrainSystem.GetFertilityModifier(lowland, world);
        var hillFactor = TerrainSystem.GetFertilityModifier(hill, world);
        var mountainFactor = TerrainSystem.GetFertilityModifier(mountain, world);

        Assert.Equal(1.0, lowlandFactor);
        Assert.True(hillFactor < lowlandFactor);
        Assert.True(mountainFactor < hillFactor);
    }

    [Fact]
    public void GetDefenseFactor_MountainsAreHarderToTake()
    {
        var lowland = BuildSettlement(FindCoordinate(Relief.Lowland));
        var mountain = BuildSettlement(FindCoordinate(Relief.Mountain));
        var world = new World { Seed = Seed };

        Assert.Equal(1.0, TerrainSystem.GetDefenseFactor(lowland, world));
        Assert.True(TerrainSystem.GetDefenseFactor(mountain, world) < TerrainSystem.GetDefenseFactor(lowland, world));
    }

    [Fact]
    public void EconomyProcess_MountainSettlement_ProducesLessFoodThanLowland()
    {
        // Проверяется не сама шкала модификатора, а то, что хозяйство её слушает
        var lowlandFood = FoodProducedAt(FindCoordinate(Relief.Lowland));
        var mountainFood = FoodProducedAt(FindCoordinate(Relief.Mountain));

        Assert.True(lowlandFood > mountainFood, $"низина должна прокормить больше, чем горы: {lowlandFood} против {mountainFood}");
    }

    [Fact]
    public void WarProcess_MountainSettlement_LosesFewerPeopleThanLowland()
    {
        // Проверяется не сама шкала модификатора, а то, что осада её слушает
        var lowlandCasualties = CasualtiesUnderSiege(FindCoordinate(Relief.Lowland));
        var mountainCasualties = CasualtiesUnderSiege(FindCoordinate(Relief.Mountain));

        Assert.True(mountainCasualties < lowlandCasualties,
            $"горы должны спасать больше жизней при осаде: {mountainCasualties} против {lowlandCasualties}");
    }

    private static int CasualtiesUnderSiege((double X, double Y) coordinate)
    {
        var world = new World { CurrentYear = 100, Seed = Seed };

        var disputed = new Settlement { Id = 1, Name = "Спорная", X = coordinate.X, Y = coordinate.Y };
        world.Settlements.Add(disputed);

        for (var i = 0; i < 60; i++)
        {
            var farmer = new Character
            {
                Id = i + 1,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = disputed,
                Profession = "Фермер"
            };

            disputed.Members.Add(farmer);
            world.Characters.Add(farmer);
        }

        foreach (var id in new[] { 1, 2 })
        {
            var seat = new Settlement { Id = 10 + id, Name = $"Столица{id}" };

            var ruler = new Character
            {
                Id = 1000 + id,
                Name = $"Правитель{id}",
                LastName = "Тестов",
                Age = 40,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = seat,
                Profession = "Фермер"
            };

            seat.Members.Add(ruler);
            world.Characters.Add(ruler);
            world.Settlements.Add(seat);

            world.Kingdoms.Add(new Kingdom
            {
                Id = id,
                Name = $"Королевство{id}",
                Dynasty = new Dynasty { Id = id, Name = $"Дом{id}", Founder = ruler, FoundedYear = 1 },
                Ruler = ruler,
                FoundedYear = 1,
                Settlements = [seat, disputed],
                Capital = seat
            });
        }

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 20; year++)
        {
            world.CurrentYear = 100 + year;
            WarSystem.Process(world);
        }

        return world.Characters.Count(c => !c.Alive && c.DeathReason == DeathReason.War);
    }

    private static double FoodProducedAt((double X, double Y) coordinate)
    {
        var world = new World { CurrentYear = 1, Seed = Seed };
        var settlement = BuildSettlement(coordinate);

        for (var i = 0; i < 10; i++)
        {
            var farmer = new Character
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

            settlement.Members.Add(farmer);
            world.Characters.Add(farmer);
        }

        world.Settlements.Add(settlement);

        EconomySystem.Process(world);

        return settlement.FoodStock;
    }

    private static Settlement BuildSettlement((double X, double Y) coordinate)
    {
        return new Settlement { Id = 1, Name = "Тестовое", X = coordinate.X, Y = coordinate.Y };
    }

    // Ищет на карте точку заданного рельефа при фиксированном зерне — тем самым
    // тестам не приходится подбирать координаты вручную под конкретную формулу шума.
    // Широта (Y) держится неизменной — иначе разница в плодородии климата
    // (см. ClimateSystem) подмешалась бы к разнице от одного только рельефа
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
