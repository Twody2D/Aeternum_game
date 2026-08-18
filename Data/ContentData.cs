using System.Text.Json;
using System.Text.Json.Serialization;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Data;

// Загружает игровой контент (имена, профессии, культуры, религии, названия поселений)
// из JSON-файлов рядом с исполняемым файлом — вместо хардкода в генераторах/системах.
// Позволяет дизайнеру редактировать контент без C# и пересборки
public static class ContentData
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static NamesData Names { get; } = Load<NamesData>("Names.json");
    public static string[] SettlementNames { get; } = Load<SettlementsData>("Settlements.json").Names;
    public static List<ProfessionEntry> Professions { get; } = Load<List<ProfessionEntry>>("Professions.json");
    public static List<CultureEntry> Cultures { get; } = Load<List<CultureEntry>>("Cultures.json");
    public static List<ReligionEntry> Religions { get; } = Load<List<ReligionEntry>>("Religions.json");

    private static T Load<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new InvalidDataException($"Файл контента повреждён или пуст: {path}");
    }
}

public class NamesData
{
    public string[] MaleNames { get; set; } = [];
    public string[] FemaleNames { get; set; } = [];
    public string[] LastNames { get; set; } = [];
}

public class SettlementsData
{
    public string[] Names { get; set; } = [];
}

public class ProfessionEntry
{
    public string Name { get; set; } = "";
    public ProfessionCategory Category { get; set; }
    public bool Hazardous { get; set; }
}

public class CultureEntry
{
    public string Name { get; set; } = "";
    public ProfessionCategory PreferredCategory { get; set; }
}

public class ReligionEntry
{
    public string Name { get; set; } = "";
}
