using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Список профессий, их категории и производство еды — профессия больше не
// декоративная строка, а часть экономики мира (см. EconomySystem).
// Сам список профессий (имя/категория/опасность) — из Data/Professions.json
public static class ProfessionSystem
{
    private static readonly Random Random = new();

    public static string school = "Школьник"; // Профессия для возраста 7 лет

    // Категория каждой профессии из ProfessionsList
    private static readonly Dictionary<string, ProfessionCategory> Categories =
        ContentData.Professions.ToDictionary(p => p.Name, p => p.Category);

    // Сколько условной еды в год производит один взрослый работник данной категории
    private static readonly Dictionary<ProfessionCategory, double> FoodProductionByCategory = new()
    {
        [ProfessionCategory.FoodProducer] = 4.0,
        [ProfessionCategory.General] = 2.5,
        [ProfessionCategory.Craft] = 2.0,
        [ProfessionCategory.Trade] = 2.0,
        [ProfessionCategory.Knowledge] = 1.0,
        [ProfessionCategory.Military] = 1.0,
    };

    // Сколько условных материалов в год производит один взрослый работник данной категории —
    // пока только ремесленники и разнорабочие, тратить материалы пока не на что (см. EconomySystem)
    private static readonly Dictionary<ProfessionCategory, double> MaterialProductionByCategory = new()
    {
        [ProfessionCategory.Craft] = 3.0,
        [ProfessionCategory.General] = 0.5,
        [ProfessionCategory.FoodProducer] = 0.0,
        [ProfessionCategory.Trade] = 0.0,
        [ProfessionCategory.Knowledge] = 0.0,
        [ProfessionCategory.Military] = 0.0,
    };

    // Профессии с повышенным риском несчастного случая — используется DeathSystem
    private static readonly HashSet<string> HazardousProfessions = ContentData.Professions
        .Where(p => p.Hazardous)
        .Select(p => p.Name)
        .ToHashSet();

    public static readonly string[] ProfessionsList = Categories.Keys.ToArray();

    // Профессии, сгруппированные по категории — для культурного смещения в GetRandom
    private static readonly Dictionary<ProfessionCategory, string[]> ProfessionsByCategory = Categories
        .GroupBy(kv => kv.Value)
        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

    // Шанс, что профессия будет выбрана из предпочитаемой культурой категории, а не из всего списка
    private const double CulturePreferenceChance = 0.5;

    // Случайная профессия. Если задана культура — с повышенным шансом выбирает
    // профессию из её предпочитаемой категории (см. Culture.PreferredCategory)
    public static string GetRandom(Culture? culture = null)
    {
        if (culture != null &&
            Random.NextDouble() < CulturePreferenceChance &&
            ProfessionsByCategory.TryGetValue(culture.PreferredCategory, out var preferred))
        {
            return preferred[Random.Next(preferred.Length)];
        }

        return ProfessionsList[
            Random.Next(ProfessionsList.Length)
        ];
    }

    public static ProfessionCategory GetCategory(string? profession)
    {
        if (profession != null && Categories.TryGetValue(profession, out var category))
        {
            return category;
        }

        return ProfessionCategory.General;
    }

    // Годовое производство еды одним взрослым работником этой профессии
    public static double GetFoodProduction(string? profession)
    {
        return FoodProductionByCategory[GetCategory(profession)];
    }

    // Годовое производство материалов одним взрослым работником этой профессии
    public static double GetMaterialProduction(string? profession)
    {
        return MaterialProductionByCategory[GetCategory(profession)];
    }

    public static bool IsHazardous(string? profession)
    {
        return profession != null && HazardousProfessions.Contains(profession);
    }
}
