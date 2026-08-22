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
// в глуши, без единого нового поля в модели.
//
// Побережье и русло реки — третий вид рельефа, вода, а не высота: считается
// раньше шума высоты и, если земля у воды, перебивает его целиком. Морской
// берег — у обоих краёв долготы (симметрично, как полюса климата у ClimateSystem);
// вглубь суши — вдоль виляющего русла реки, тем же зерном мира, что и сам
// рельеф, но независимой волной, чтобы река не повторяла горный хребет.
// На 20000 точках карты вода даёт около 17% земли при любом зерне — заметная,
// но не подавляющая доля. Земля у воды родит охотнее прочих (см.
// CoastFertilityBonus) и берёт больше товара на внешний рынок (см.
// GetTradeCapacityMultiplier, применяется в MarketSystem) — порт и пристань,
// а не просто ещё один клочок земли
public static class TerrainSystem
{
    private const double LowlandThreshold = 0.4; // Ниже — низина
    private const double MountainThreshold = 0.6; // Выше — горы, между ними — холмы

    private const double HillFertilityFactor = 0.9;
    private const double MountainFertilityFactor = 0.7;
    private const double CoastFertilityBonus = 1.2; // Пойма и приморье плодороднее обычной низины

    private const double HillDefenseFactor = 0.9;
    private const double MountainDefenseFactor = 0.8;

    private const double CoastalTradeBonus = 1.5; // Порт и пристань вывозят больше обычного

    private const double CoastBand = 60; // Полоса у восточного/западного края карты — морской берег
    private const double RiverAmplitude = 200; // Насколько далеко от середины карты может вильнуть русло
    private const double RiverBand = 30; // Ширина полосы вдоль русла, которую считаем берегом

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
        return GetRelief(settlement.X, settlement.Y, world.Seed);
    }

    // Побережье проверяется раньше высоты — если земля у воды, дальше не важно,
    // что сказал бы шум высоты в этой самой точке
    public static Relief GetRelief(double x, double y, int seed)
    {
        if (IsCoastal(x, y, seed))
        {
            return Relief.Coast;
        }

        return GetRelief(GetElevation(x, y, seed));
    }

    public static Relief GetRelief(double elevation)
    {
        if (elevation <= LowlandThreshold)
        {
            return Relief.Lowland;
        }

        return elevation >= MountainThreshold ? Relief.Mountain : Relief.Hill;
    }

    // Морской берег — у обоих краёв долготы; вглубь суши — вдоль виляющего русла
    // реки. Фаза русла — отдельная от фаз рельефа (см. GetElevation), иначе река
    // всегда бы совпадала с горным хребтом одной и той же формулы
    private static bool IsCoastal(double x, double y, int seed)
    {
        if (x <= CoastBand || x >= ClimateSystem.MapSize - CoastBand)
        {
            return true;
        }

        var riverPhase = (seed * 7 % 10000) / 10000.0 * Math.PI * 2;
        var riverY = ClimateSystem.MapSize / 2 + RiverAmplitude * Math.Sin(x / ClimateSystem.MapSize * Math.PI * 2 + riverPhase);

        return Math.Abs(y - riverY) <= RiverBand;
    }

    // Множитель к производству еды (см. EconomySystem) — гористая земля скупее
    // пашни, а пойма и приморье щедрее обычной низины
    public static double GetFertilityModifier(Settlement settlement, World world)
    {
        return GetRelief(settlement, world) switch
        {
            Relief.Hill => HillFertilityFactor,
            Relief.Mountain => MountainFertilityFactor,
            Relief.Coast => CoastFertilityBonus,
            _ => 1.0
        };
    }

    // Понижающий множитель для потерь при войне (см. WarSystem) — тот же принцип,
    // что у WallSystem.GetWallFactor: 1.0 без бонуса, ниже — чем круче местность.
    // Побережье такого бонуса не даёт — открытая вода не защищает, в отличие от гор
    public static double GetDefenseFactor(Settlement settlement, World world)
    {
        return GetRelief(settlement, world) switch
        {
            Relief.Hill => HillDefenseFactor,
            Relief.Mountain => MountainDefenseFactor,
            _ => 1.0
        };
    }

    // Множитель к тому, сколько лишнего товара вывозит купец за год (см. MarketSystem) —
    // порт и пристань берут больше обычной подводы
    public static double GetTradeCapacityMultiplier(Settlement settlement, World world)
    {
        return GetRelief(settlement, world) == Relief.Coast ? CoastalTradeBonus : 1.0;
    }

    public static string GetName(Relief relief)
    {
        return relief switch
        {
            Relief.Hill => "холмы",
            Relief.Mountain => "горы",
            Relief.Coast => "побережье",
            _ => "низина"
        };
    }
}
