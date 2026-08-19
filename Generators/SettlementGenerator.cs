using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика поселений — как стартовых, так и основанных позже колонизацией
// (см. ColonizationSystem). Имена — из Data/Settlements.json (см. ContentData)
public static class SettlementGenerator
{
    private static readonly Random _random = new();

    private static int _nextId = 1;

    private static string[] Names => ContentData.SettlementNames;

    private const double MapSize = 1000; // Условная карта, на которой размещаются поселения
    private const double ColonyOffsetRange = 50; // Насколько далеко от родителя может оказаться новая колония

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
                ? (Clamp(origin.X + RandomOffset()), Clamp(origin.Y + RandomOffset()))
                : (_random.NextDouble() * MapSize, _random.NextDouble() * MapSize);

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

    private static double RandomOffset()
    {
        return (_random.NextDouble() * 2 - 1) * ColonyOffsetRange;
    }

    private static double Clamp(double value)
    {
        return Math.Clamp(value, 0, MapSize);
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
