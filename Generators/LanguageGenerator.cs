using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Generators;

// Языков в мире меньше, чем народов: несколько родственных культур говорят
// на одном наречии. Именно поэтому языковая граница не совпадает с культурной —
// иначе язык был бы просто вторым именем культуры и ничего нового не значил
public static class LanguageGenerator
{
    private const int CulturesPerLanguage = 2; // Сколько народов в среднем делят одно наречие

    private static int _nextId = 1;

    private static readonly string[] Names =
    {
        "Старое наречие",
        "Речь долин",
        "Горский говор",
        "Приморская речь",
        "Лесное наречие",
        "Степной говор"
    };

    public static List<Language> Create(int cultureCount)
    {
        // Округление вверх, а не вниз: при трёх народах (столько их в мире
        // по умолчанию) деление нацело дало бы единственное наречие на всех,
        // и никакого барьера в мире просто не возникло бы
        var count = Math.Max(1, (int)Math.Ceiling(cultureCount / (double)CulturesPerLanguage));
        var languages = new List<Language>();

        for (var i = 0; i < count; i++)
        {
            languages.Add(new Language
            {
                Id = _nextId++,
                Name = Names[i % Names.Length]
            });
        }

        return languages;
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}
