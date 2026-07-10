namespace Aeternum.WorldGen.Characters;

public class ProfessionGenerator
{
    private static readonly Random Random = new();

    public static string GetRandom()
    {
        return ProfessionsList[
            Random.Next(ProfessionsList.Length)
        ];
    }
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
}