using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Климат: до сих пор координаты поселения (Settlement.X/Y) работали ровно в одном
// месте — как расстояние при выборе направления переезда (MigrationSystem). Здесь
// они начинают задавать саму землю: середина карты — умеренный пояс с щедрым
// урожаем, края (север и юг) — скупая земля. Ничего не хранится и не сохраняется:
// плодородие целиком выводится из координат, поэтому не может разъехаться с картой
public static class ClimateSystem
{
    public const double MapSize = 1000; // Условная карта, на которой размещаются поселения

    private const double MinFertility = 0.6; // Край карты — суровый климат
    private const double MaxFertility = 1.3; // Середина карты — умеренный пояс

    // Множитель производства еды для поселения (см. EconomySystem)
    public static double GetFertility(Settlement settlement)
    {
        return GetFertility(settlement.Y);
    }

    public static double GetFertility(double y)
    {
        // 0 в середине карты, 1 у самого края — насколько далеко от умеренного пояса
        var distanceFromTemperate = Math.Abs(y - MapSize / 2) / (MapSize / 2);

        return MaxFertility - (MaxFertility - MinFertility) * distanceFromTemperate;
    }
}
