using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Список профессий, их категории и производство еды — профессия больше не
// декоративная строка, а часть экономики мира (см. EconomySystem).
// Сам список профессий (имя/категория/опасность) — из Data/Professions.json
public static class ProfessionSystem
{
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

    // Сколько золота в год производит один взрослый работник данной категории — только Trade
    // (торговец, купец и т.п.); остальные категории не производят золото
    private static readonly Dictionary<ProfessionCategory, double> GoldProductionByCategory = new()
    {
        [ProfessionCategory.Trade] = 3.0,
    };

    // Какой тип материала производит каждая конкретная ремесленная профессия —
    // привязка один-в-один, а не по грубой категории (см. MaterialType)
    private static readonly Dictionary<string, MaterialType> MaterialTypeByProfession = new()
    {
        ["Кузнец"] = MaterialType.Metal,
        ["Оружейник"] = MaterialType.Metal,
        ["Столяр"] = MaterialType.Wood,
        ["Строитель"] = MaterialType.Wood,
        ["Каменщик"] = MaterialType.Stone,
        ["Ткач"] = MaterialType.Textile,
        ["Портной"] = MaterialType.Textile,
        ["Сапожник"] = MaterialType.Textile,
        ["Кожевник"] = MaterialType.Textile,
        ["Гончар"] = MaterialType.Clay,
        ["Стекольщик"] = MaterialType.Clay,
        ["Ювелир"] = MaterialType.Clay,
        ["Ремесленник"] = MaterialType.Clay,
    };

    private const double CraftMaterialAmount = 3.0; // Годовое производство материала одним ремесленником своего типа
    private const double GeneralMaterialAmount = 0.5; // Разнорабочие производят немного неспециализированного материала

    // Профессии, которые гарантированно должны быть хотя бы у одного живого жителя
    // поселения — по одному представителю на еду и на каждый тип материала.
    // Без этого поселение могло случайно остаться совсем без кузнеца или земледельца
    private static readonly string[] EssentialProfessions =
    {
        "Фермер", "Кузнец", "Столяр", "Каменщик", "Ткач", "Гончар"
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

    // Шанс, что персонаж унаследует профессию родителя своего пола вместо случайного выбора —
    // семейное ремесло/дело передаётся из поколения в поколение, но не всегда (см. LifeSystem)
    private const double InheritanceChance = 0.4;

    // Шанс на профессию категории Knowledge за одну школу в поселении (см. SchoolSystem)
    private const double SchoolBonusPerSchool = 0.1;
    private const double MaxSchoolBonus = 0.4;

    // Случайная профессия. Если задано поселение и в нём не хватает одной из
    // обязательных профессий (см. EssentialProfessions) — гарантированно выбирает
    // именно её. Иначе, если задана профессия родителя — с некоторым шансом
    // наследует её. Иначе, если в поселении есть школы — с некоторым шансом
    // выбирает профессию категории Knowledge. Иначе, если задана культура —
    // с повышенным шансом выбирает профессию из её предпочитаемой категории
    // (см. Culture.PreferredCategory)
    public static string GetRandom(Culture? culture = null, Settlement? settlement = null, string? inheritedProfession = null)
    {
        if (settlement != null)
        {
            var missing = GetMissingEssentialProfessions(settlement);

            if (missing.Count > 0)
            {
                return missing[Rng.Next(missing.Count)];
            }
        }

        if (inheritedProfession != null &&
            Categories.ContainsKey(inheritedProfession) &&
            Rng.NextDouble() < InheritanceChance)
        {
            return inheritedProfession;
        }

        if (settlement is { Schools: > 0 } &&
            Rng.NextDouble() < Math.Min(MaxSchoolBonus, settlement.Schools * SchoolBonusPerSchool) &&
            ProfessionsByCategory.TryGetValue(ProfessionCategory.Knowledge, out var knowledgeProfessions))
        {
            return knowledgeProfessions[Rng.Next(knowledgeProfessions.Length)];
        }

        if (culture != null &&
            Rng.NextDouble() < CulturePreferenceChance &&
            ProfessionsByCategory.TryGetValue(culture.PreferredCategory, out var preferred))
        {
            return preferred[Rng.Next(preferred.Length)];
        }

        return ProfessionsList[
            Rng.Next(ProfessionsList.Length)
        ];
    }

    // Обязательные профессии, которых сейчас нет ни у одного живого жителя поселения
    private static List<string> GetMissingEssentialProfessions(Settlement settlement)
    {
        var present = settlement.Members
            .Where(m => m.Alive)
            .Select(m => m.Profession)
            .ToHashSet();

        return EssentialProfessions.Where(p => !present.Contains(p)).ToList();
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

    // Годовое производство золота одним взрослым работником этой профессии
    public static double GetGoldProduction(string? profession)
    {
        return GoldProductionByCategory.GetValueOrDefault(GetCategory(profession));
    }

    // Годовое производство материалов одним взрослым работником этой профессии: тип и количество
    public static (MaterialType Type, double Amount) GetMaterialProduction(string? profession)
    {
        if (profession != null && MaterialTypeByProfession.TryGetValue(profession, out var type))
        {
            return (type, CraftMaterialAmount);
        }

        if (GetCategory(profession) == ProfessionCategory.General)
        {
            return (MaterialType.General, GeneralMaterialAmount);
        }

        return (MaterialType.General, 0.0);
    }

    public static bool IsHazardous(string? profession)
    {
        return profession != null && HazardousProfessions.Contains(profession);
    }
}
