namespace Aeternum.WorldGen.Systems;

// Список взрослых профессий и выбор случайной из них
public static class ProfessionSystem
{
    private static readonly Random Random = new();

    // Случайная профессия из ProfessionsList
    public static string GetRandom()
    {
        return ProfessionsList[
            Random.Next(ProfessionsList.Length)
        ];
    }

    public static string school = "Школьник"; // Профессия для возраста 7 лет
    public static readonly string[] ProfessionsList =
    {
        "Кузнец",
        "Деревенский житель",
        "Торговец",
        "Сельский житель",
        "Воин",
        "Маг",
        "Охотник",
        "Ремесленник",
        "Фермер",
        "Рыбак",
        "Пастух",
        "Пекарь",
        "Портной",
        "Строитель",
        "Мельник",
        "Пастырь",
        "Лекарь",
        "Музыкант",
        "Писатель",
        "Учёный",
        "Путешественник",
        "Артист",
        "Садовник",
        "Купец",
        "Солдат",
        "Офицер",
        "Моряк",
        "Повар",
        "Сапожник",
        "Ткач",
        "Каменщик",
        "Столяр",
    };

    // Профессии с повышенным риском несчастного случая — используется DeathSystem
    private static readonly HashSet<string> HazardousProfessions = new()
    {
        "Воин",
        "Охотник",
        "Солдат",
        "Офицер",
        "Моряк"
    };

    public static bool IsHazardous(string? profession)
    {
        return profession != null && HazardousProfessions.Contains(profession);
    }
}