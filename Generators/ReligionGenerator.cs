using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Фабрика религий. По образцу CultureGenerator — пул шаблонов названий
// из Data/Religions.json (см. ContentData)
public static class ReligionGenerator
{
    private static int _nextId = 1;

    private static List<ReligionEntry> Templates => ContentData.Religions;

    public static List<Religion> Create(int count)
    {
        var religions = new List<Religion>();

        for (int i = 0; i < count; i++)
        {
            religions.Add(new Religion
            {
                Id = _nextId++,
                Name = Templates[i % Templates.Count].Name
            });
        }

        return religions;
    }

    private const string SchismMarker = " (толк:";

    // Новая вера, отколовшаяся от материнской в конкретном поселении (см. SchismSystem).
    // Имя показывает происхождение, а не заменяет его: раскол — это ветвь, а не
    // чужая вера с нуля. Название поселения не склоняем — та же договорённость,
    // что и у остальных имён собственных в мире.
    //
    // Считаем от корня, а не от прямого родителя: толк тоже может расколоться, и
    // приписывание нового суффикса к прежнему давало бы "Культ огня (толк: X)
    // (толк: X)". Год отделяет повторные расколы одного и того же поселения друг
    // от друга — иначе они получили бы совпадающие имена
    public static Religion CreateSchism(Religion parent, Settlement birthplace, int year)
    {
        var markerIndex = parent.Name.IndexOf(SchismMarker, StringComparison.Ordinal);
        var rootName = markerIndex < 0 ? parent.Name : parent.Name[..markerIndex];

        return new Religion
        {
            Id = _nextId++,
            Name = $"{rootName} (толк: {birthplace.Name}, {year})"
        };
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
