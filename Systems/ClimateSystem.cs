using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Климат: до сих пор координаты поселения (Settlement.X/Y) работали ровно в одном
// месте — как расстояние при выборе направления переезда (MigrationSystem). Здесь
// они начинают задавать саму землю: середина карты — умеренный пояс с щедрым
// урожаем, края (север и юг) — скупая земля. Ничего не хранится и не сохраняется:
// плодородие целиком выводится из координат, поэтому не может разъехаться с картой.
//
// Пояс не стоит на месте вечно: поверх годовой погоды (WeatherSystem — общий
// множитель на год, тот же для всей карты) идёт куда более медленный снос самой
// середины пояса (World.ClimateDrift) — блуждание не годами, а веками, без
// возврата к нулю (в отличие от WeatherSystem, которое тянется к среднему году
// из года в год). Земля, бывшая срединной сотню лет назад, может на глазах
// поколений оскудеть — не разовый неурожай (DisasterSystem), а растянутый
// на века повод сняться с места (MigrationSystem слушает не пояс напрямую,
// а FoodStock — тот, что уже испёк в себе и погоду, и климат, и рельеф)
public static class ClimateSystem
{
    public const double MapSize = 1000; // Условная карта, на которой размещаются поселения

    private const double MinFertility = 0.6; // Край карты — суровый климат
    private const double MaxFertility = 1.3; // Середина карты — умеренный пояс

    private const double DriftStepSize = 3.0; // Насколько может качнуться пояс за один год — счёт на века, не на годы (см. WeatherSystem.StepSize)
    private const double MaxDrift = 200; // Дальше пояс не уходит — пятая часть карты в любую сторону, не переворот климата целиком

    // Снос пояса за этот год — чистое блуждание без тяги к нулю: в отличие от
    // WeatherSystem, климату незачем возвращаться туда, где он был при основании мира
    public static void Process(World world)
    {
        var step = (Rng.NextDouble() * 2 - 1) * DriftStepSize;

        world.ClimateDrift = Math.Clamp(world.ClimateDrift + step, -MaxDrift, MaxDrift);
    }

    // Множитель производства еды для поселения (см. EconomySystem) с учётом
    // векового сноса пояса — тот же расчёт, что и без него, просто от смещённой середины
    public static double GetFertility(Settlement settlement, World world)
    {
        return GetFertility(settlement.Y - world.ClimateDrift);
    }

    // То же самое без сноса пояса — для мест, куда World ещё не дошёл (выбор
    // места при основании поселения, см. SettlementGenerator, — мир на тот
    // момент только создаётся) или где тянуть World через сигнатуру ради
    // небольшой поправки непропорционально дороже самой поправки (тяга к земле
    // при выборе ремесла, см. ProfessionSystem.TryPickFarming)
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
