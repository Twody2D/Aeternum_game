using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика поселений — как стартовых, так и основанных позже колонизацией
// (см. ColonizationSystem). Имена — из Data/Settlements.json (см. ContentData)
public static class SettlementGenerator
{
    private static int _nextId = 1;

    private static string[] Names => ContentData.SettlementNames;

    public static List<Settlement> Create(int count)
    {
        var settlements = new List<Settlement>();

        for (int i = 0; i < count; i++)
        {
            // Имя берём по фактическому Id, а не по индексу цикла — иначе при
            // повторных вызовах Create (колонизация зовёт его много раз за игру)
            // каждый новый посёлок получал бы одно и то же первое имя
            var id = _nextId++;

            settlements.Add(new Settlement
            {
                Id = id,
                Name = Names[(id - 1) % Names.Length]
            });
        }

        return settlements;
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
