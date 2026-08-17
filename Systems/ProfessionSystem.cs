using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Список профессий, их категории и производство еды — профессия больше не
// декоративная строка, а часть экономики мира (см. EconomySystem)
public static class ProfessionSystem
{
    private static readonly Random Random = new();

    public static string school = "Школьник"; // Профессия для возраста 7 лет

    // Категория каждой профессии из ProfessionsList
    private static readonly Dictionary<string, ProfessionCategory> Categories = new()
    {
        // Производители еды
        ["Фермер"] = ProfessionCategory.FoodProducer,
        ["Рыбак"] = ProfessionCategory.FoodProducer,
        ["Пастух"] = ProfessionCategory.FoodProducer,
        ["Пекарь"] = ProfessionCategory.FoodProducer,
        ["Мельник"] = ProfessionCategory.FoodProducer,
        ["Охотник"] = ProfessionCategory.FoodProducer,
        ["Садовник"] = ProfessionCategory.FoodProducer,
        ["Виноградарь"] = ProfessionCategory.FoodProducer,
        ["Пчеловод"] = ProfessionCategory.FoodProducer,
        ["Мясник"] = ProfessionCategory.FoodProducer,
        ["Птицевод"] = ProfessionCategory.FoodProducer,
        ["Свинопас"] = ProfessionCategory.FoodProducer,

        // Ремесленники и строители
        ["Кузнец"] = ProfessionCategory.Craft,
        ["Портной"] = ProfessionCategory.Craft,
        ["Ткач"] = ProfessionCategory.Craft,
        ["Сапожник"] = ProfessionCategory.Craft,
        ["Столяр"] = ProfessionCategory.Craft,
        ["Каменщик"] = ProfessionCategory.Craft,
        ["Строитель"] = ProfessionCategory.Craft,
        ["Ремесленник"] = ProfessionCategory.Craft,
        ["Гончар"] = ProfessionCategory.Craft,
        ["Ювелир"] = ProfessionCategory.Craft,
        ["Кожевник"] = ProfessionCategory.Craft,
        ["Стекольщик"] = ProfessionCategory.Craft,
        ["Оружейник"] = ProfessionCategory.Craft,

        // Торговля
        ["Торговец"] = ProfessionCategory.Trade,
        ["Купец"] = ProfessionCategory.Trade,
        ["Трактирщик"] = ProfessionCategory.Trade,
        ["Извозчик"] = ProfessionCategory.Trade,
        ["Ростовщик"] = ProfessionCategory.Trade,

        // Военные и опасные профессии
        ["Воин"] = ProfessionCategory.Military,
        ["Солдат"] = ProfessionCategory.Military,
        ["Офицер"] = ProfessionCategory.Military,
        ["Моряк"] = ProfessionCategory.Military,
        ["Стражник"] = ProfessionCategory.Military,
        ["Разведчик"] = ProfessionCategory.Military,
        ["Наёмник"] = ProfessionCategory.Military,

        // Знания и услуги
        ["Лекарь"] = ProfessionCategory.Knowledge,
        ["Учёный"] = ProfessionCategory.Knowledge,
        ["Маг"] = ProfessionCategory.Knowledge,
        ["Писатель"] = ProfessionCategory.Knowledge,
        ["Музыкант"] = ProfessionCategory.Knowledge,
        ["Артист"] = ProfessionCategory.Knowledge,
        ["Пастырь"] = ProfessionCategory.Knowledge,
        ["Алхимик"] = ProfessionCategory.Knowledge,
        ["Астроном"] = ProfessionCategory.Knowledge,
        ["Летописец"] = ProfessionCategory.Knowledge,
        ["Философ"] = ProfessionCategory.Knowledge,

        // Разнорабочие без узкой специализации
        ["Деревенский житель"] = ProfessionCategory.General,
        ["Сельский житель"] = ProfessionCategory.General,
        ["Путешественник"] = ProfessionCategory.General,
        ["Повар"] = ProfessionCategory.General,
        ["Слуга"] = ProfessionCategory.General,
        ["Батрак"] = ProfessionCategory.General,
        ["Бродяга"] = ProfessionCategory.General,
    };

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

    // Профессии с повышенным риском несчастного случая — используется DeathSystem
    private static readonly HashSet<string> HazardousProfessions = new()
    {
        "Воин",
        "Охотник",
        "Солдат",
        "Офицер",
        "Моряк",
        "Стражник",
        "Разведчик",
        "Наёмник"
    };

    public static readonly string[] ProfessionsList = Categories.Keys.ToArray();

    // Случайная профессия из ProfessionsList
    public static string GetRandom()
    {
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

    public static bool IsHazardous(string? profession)
    {
        return profession != null && HazardousProfessions.Contains(profession);
    }
}
