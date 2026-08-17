using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика поселений: создаёт стартовые деревни с уникальными именами
public static class SettlementGenerator
{
    private static int _nextId = 1;

    private static readonly string[] Names =
    {
        "Дубрава",
        "Заречье",
        "Ольховка",
        "Северный Дол",
        "Липовка",
        "Берёзовка",
        "Сосновка",
        "Гореловка",
        "Каменный Брод",
        "Тихий Лог",
        "Ясная Поляна",
        "Червонное"
    };

    public static List<Settlement> Create(int count)
    {
        var settlements = new List<Settlement>();

        for (int i = 0; i < count; i++)
        {
            settlements.Add(new Settlement
            {
                Id = _nextId++,
                Name = Names[i % Names.Length]
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
