using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

public class ClimateSystemTests
{
    [Fact]
    public void GetFertility_MiddleOfMap_IsRicherThanEdges()
    {
        var middle = ClimateSystem.GetFertility(ClimateSystem.MapSize / 2);
        var north = ClimateSystem.GetFertility(0);
        var south = ClimateSystem.GetFertility(ClimateSystem.MapSize);

        Assert.True(middle > north);
        Assert.True(middle > south);
    }

    [Fact]
    public void GetFertility_IsSymmetricAroundTemperateBelt()
    {
        // Север и юг одинаково суровы — иначе колонизация ползла бы в одну сторону
        // не из-за климата, а из-за перекоса самой формулы
        var north = ClimateSystem.GetFertility(ClimateSystem.MapSize / 2 - 200);
        var south = ClimateSystem.GetFertility(ClimateSystem.MapSize / 2 + 200);

        Assert.Equal(north, south, precision: 10);
    }

    [Fact]
    public void GetFertility_StaysPositiveEverywhere()
    {
        // Ноль или отрицательное плодородие означало бы поселение, где еда в
        // принципе не родится, — такого места на карте быть не должно
        for (double y = 0; y <= ClimateSystem.MapSize; y += 50)
        {
            Assert.True(ClimateSystem.GetFertility(y) > 0, $"плодородие на Y={y} должно быть положительным");
        }
    }

    [Fact]
    public void GetFertility_FallsMonotonicallyFromMiddleToEdge()
    {
        var previous = ClimateSystem.GetFertility(ClimateSystem.MapSize / 2);

        for (double y = ClimateSystem.MapSize / 2 + 50; y <= ClimateSystem.MapSize; y += 50)
        {
            var current = ClimateSystem.GetFertility(y);

            Assert.True(current < previous, $"плодородие должно падать к краю, но на Y={y} выросло");
            previous = current;
        }
    }

    // Снос пояса (World.ClimateDrift) — куда более медленное блуждание, чем
    // погода года, и без тяги обратно к нулю (в отличие от WeatherSystem)
    [Fact]
    public void Process_Drift_StaysWithinBounds()
    {
        // Много лет и несколько зёрен — без потолка блуждание без тяги к нулю
        // рано или поздно пересекло бы любую границу (возвратность случайного
        // блуждания), 2000 лет на одном зерне для этого маловато и не ловит
        // потерю Math.Clamp надёжно
        for (var seed = 1; seed <= 5; seed++)
        {
            var world = new World();

            Rng.Initialize(seed);

            for (var year = 0; year < 20_000; year++)
            {
                ClimateSystem.Process(world);
                Assert.InRange(world.ClimateDrift, -200, 200);
            }
        }
    }

    [Fact]
    public void Process_Drift_EventuallyMovesNoticeablyFromZero()
    {
        // Один конкретный прогон мог бы случайно вернуться близко к нулю за
        // 500 лет — проверяем на выборке зёрен, а не веря одному броску
        var noticeable = 0;

        for (var seed = 1; seed <= 30; seed++)
        {
            var world = new World();

            Rng.Initialize(seed);

            for (var year = 0; year < 500; year++)
            {
                ClimateSystem.Process(world);
            }

            if (Math.Abs(world.ClimateDrift) > 10)
            {
                noticeable++;
            }
        }

        Assert.True(noticeable > 20,
            $"за 500 лет блуждания пояс должен заметно сдвинуться от нуля хотя бы в большинстве зёрен: {noticeable} из 30");
    }

    [Fact]
    public void GetFertility_WithSettlementAndWorld_ShiftsBeltByDrift()
    {
        var world = new World { ClimateDrift = 200 };
        var settlement = new Settlement { Id = 1, Name = "Тест", Y = ClimateSystem.MapSize / 2 + 200 };

        // Пояс сместился ровно туда, где стоит поселение, — плодородие должно
        // совпасть с тем, что было бы у самой середины карты без сноса
        var shifted = ClimateSystem.GetFertility(settlement, world);
        var unshiftedMiddle = ClimateSystem.GetFertility(ClimateSystem.MapSize / 2);

        Assert.Equal(unshiftedMiddle, shifted, precision: 10);
    }

    [Fact]
    public void GetFertility_NoDrift_MatchesFertilityWithoutWorld()
    {
        var world = new World(); // ClimateDrift = 0 по умолчанию
        var settlement = new Settlement { Id = 1, Name = "Тест", Y = 300 };

        Assert.Equal(ClimateSystem.GetFertility(settlement), ClimateSystem.GetFertility(settlement, world), precision: 10);
    }
}
