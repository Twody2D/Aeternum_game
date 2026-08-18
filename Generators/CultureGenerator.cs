using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика культур: у каждой — своё название и предпочитаемая категория профессий.
// Шаблоны — из Data/Cultures.json (см. ContentData)
public static class CultureGenerator
{
    private static int _nextId = 1;

    private static List<CultureEntry> Templates => ContentData.Cultures;

    public static List<Culture> Create(int count)
    {
        var cultures = new List<Culture>();

        for (int i = 0; i < count; i++)
        {
            var template = Templates[i % Templates.Count];

            cultures.Add(new Culture
            {
                Id = _nextId++,
                Name = template.Name,
                PreferredCategory = template.PreferredCategory
            });
        }

        return cultures;
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
