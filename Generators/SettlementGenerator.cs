using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Generators;

// Фабрика поселений — как стартовых, так и основанных позже колонизацией
// (см. ColonizationSystem). Имена — из Data/Settlements.json (см. ContentData)
public static class SettlementGenerator
{
    private static int _nextId = 1;

    private static string[] Names => ContentData.SettlementNames;

    private const double ColonyOffsetRange = 50; // Насколько далеко от родителя может оказаться новая колония
    private const int ColonySiteCandidates = 3; // Сколько мест колонисты осматривают, прежде чем осесть

    // origin задан — колония появляется рядом с родительским поселением (см. ColonizationSystem),
    // не задан — стартовая генерация раскидывает поселения случайно по всей карте
    public static List<Settlement> Create(int count, Settlement? origin = null)
    {
        var settlements = new List<Settlement>();

        for (int i = 0; i < count; i++)
        {
            // Имя берём по фактическому Id, а не по индексу цикла — иначе при
            // повторных вызовах Create (колонизация зовёт его много раз за игру)
            // каждый новый посёлок получал бы одно и то же первое имя
            var id = _nextId++;

            var (x, y) = origin != null
                ? PickColonySite(origin)
                : (Rng.NextDouble() * ClimateSystem.MapSize, Rng.NextDouble() * ClimateSystem.MapSize);

            settlements.Add(new Settlement
            {
                Id = id,
                Name = Names[(id - 1) % Names.Length],
                X = x,
                Y = y
            });
        }

        return settlements;
    }

    // Колонисты не селятся вслепую: осматривают несколько мест в округе родительского
    // поселения и оседают на самом плодородном (см. ClimateSystem) — за поколения
    // это уводит колонии от скупых краёв карты к умеренному поясу
    private static (double X, double Y) PickColonySite(Settlement origin)
    {
        var best = (X: Clamp(origin.X + RandomOffset()), Y: Clamp(origin.Y + RandomOffset()));

        for (var i = 1; i < ColonySiteCandidates; i++)
        {
            var candidate = (X: Clamp(origin.X + RandomOffset()), Y: Clamp(origin.Y + RandomOffset()));

            if (ClimateSystem.GetFertility(candidate.Y) > ClimateSystem.GetFertility(best.Y))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static double RandomOffset()
    {
        return (Rng.NextDouble() * 2 - 1) * ColonyOffsetRange;
    }

    private static double Clamp(double value)
    {
        return Math.Clamp(value, 0, ClimateSystem.MapSize);
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
