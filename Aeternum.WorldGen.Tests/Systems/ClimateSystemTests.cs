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
}
