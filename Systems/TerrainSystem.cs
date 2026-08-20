using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Рельеф. До сих пор из двух координат поселения (Settlement.X/Y) в дело шла
// только одна — широта задавала плодородие (см. ClimateSystem), а долгота
// участвовала лишь в подсчёте расстояний. Карта была плоской: гор, холмов и
// низин на ней не было, только тёплый пояс и суровые края.
//
// Рельеф выводится из обеих координат сразу, тем же приёмом, что и климат:
// ничего не хранится, ничего не может разъехаться с картой. Несколько волн
// разной частоты и фазы, сдвинутых зерном мира, дают неровную, но воспроизводимую
// местность — не соль-перец из случайных точек (соседние клочки земли похожи
// друг на друга), а и не единую полосу, как климат. Пороги 0.4/0.6 подобраны
// по замеру: на 20000 случайных точках карты делят её примерно на 30% низин,
// 40% холмов и 30% гор при любом зерне мира.
//
// Из рельефа выходит естественный компромисс: горы и холмы — скупая земля
// (см. GetFertilityModifier, применяется в EconomySystem наравне с климатом
// и погодой), но её труднее взять силой (см. GetDefenseFactor, применяется
// в WarSystem наравне со стенами) — своя цена и своя выгода тем, кто осел
// в глуши, без единого нового поля в модели
public static class TerrainSystem
{
    private const double LowlandThreshold = 0.4; // Ниже — низина
    private const double MountainThreshold = 0.6; // Выше — горы, между ними — холмы

    private const double HillFertilityFactor = 0.9;
    private const double MountainFertilityFactor = 0.7;

    private const double HillDefenseFactor = 0.9;
    private const double MountainDefenseFactor = 0.8;

    public static double GetElevation(Settlement settlement, World world)
    {
        return GetElevation(settlement.X, settlement.Y, world.Seed);
    }

    public static double GetElevation(double x, double y, int seed)
    {
        var phaseA = (seed % 1000) / 1000.0 * Math.PI * 2;
        var phaseB = (seed / 1000 % 1000) / 1000.0 * Math.PI * 2;
        var scale = Math.PI * 2 / ClimateSystem.MapSize;

        var wave = Math.Sin(x * scale * 3 + phaseA) * Math.Cos(y * scale * 2 + phaseB)
                   + 0.5 * Math.Sin((x + y) * scale * 5 + phaseA)
                   + 0.3 * Math.Cos((x - y) * scale * 7 + phaseB);

        return Math.Clamp((wave / 1.8 + 1) / 2, 0, 1);
    }

    public static Relief GetRelief(Settlement settlement, World world)
    {
        return GetRelief(GetElevation(settlement, world));
    }

    public static Relief GetRelief(double elevation)
    {
        if (elevation <= LowlandThreshold)
        {
            return Relief.Lowland;
        }

        return elevation >= MountainThreshold ? Relief.Mountain : Relief.Hill;
    }

    // Множитель к производству еды (см. EconomySystem) — гористая земля скупее пашни
    public static double GetFertilityModifier(Settlement settlement, World world)
    {
        return GetRelief(settlement, world) switch
        {
            Relief.Hill => HillFertilityFactor,
            Relief.Mountain => MountainFertilityFactor,
            _ => 1.0
        };
    }

    // Понижающий множитель для потерь при войне (см. WarSystem) — тот же принцип,
    // что у WallSystem.GetWallFactor: 1.0 без бонуса, ниже — чем круче местность
    public static double GetDefenseFactor(Settlement settlement, World world)
    {
        return GetRelief(settlement, world) switch
        {
            Relief.Hill => HillDefenseFactor,
            Relief.Mountain => MountainDefenseFactor,
            _ => 1.0
        };
    }

    public static string GetName(Relief relief)
    {
        return relief switch
        {
            Relief.Hill => "холмы",
            Relief.Mountain => "горы",
            _ => "низина"
        };
    }
}
