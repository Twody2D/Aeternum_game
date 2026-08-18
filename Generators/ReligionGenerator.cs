using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика религий. По образцу CultureGenerator — фиксированный пул шаблонов названий
public static class ReligionGenerator
{
    private static int _nextId = 1;

    private static readonly string[] Templates =
    {
        "Культ огня",
        "Культ предков",
        "Культ земли",
        "Единобожие",
        "Культ солнца"
    };

    public static List<Religion> Create(int count)
    {
        var religions = new List<Religion>();

        for (int i = 0; i < count; i++)
        {
            religions.Add(new Religion
            {
                Id = _nextId++,
                Name = Templates[i % Templates.Length]
            });
        }

        return religions;
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
