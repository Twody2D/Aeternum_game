namespace Aeternum.WorldGen.Characters;


public static class CharacterGenerator
{
    private static readonly string[] Names =
    {
        "Артур",
        "Вильгельм",
        "Альфред",
        "Эдмунд",
        "Годрик",
        "Роберт"
    };

    // private static readonly string[] Professions =
    // {
    //    "Кузнец",
    //     "Деревенский житель",
    //     "Торговец",
    //     "Сельский житель",
    //     "Воин",
    //     "Маг",
    //     "Охотник",
    //     "Ремесленник",
    //     "Фермер",
    //     "Рыбак",
    //     "Пастух",
    //     "Пекарь",
    //     "Портной",
    //     "Строитель",
    //     "Мельник",
    //     "Пастырь",
    //     "Лекарь",
    //     "Музыкант",
    //     "Писатель",
    //     "Учёный",
    //     "Путешественник",
    //     "Артист",
    //     "Садовник",
    //     "Купец",
    //     "Солдат",
    //     "Офицер",
    //     "Моряк",
    //     "Повар",
    //     "Сапожник",
    //     "Ткач",
    //     "Каменщик",
    //     "Столяр",
    // };

    private static readonly Random Random = new(); // Генератор случайных чисел

    public static Character Create()   // Метод для создания нового персонажа
    {              
        
        //profession = TakeProfession.ProfessionsList[Random.Next(TakeProfession.ProfessionsList.Length)]; // Случайная профессия из массива Professions    
        return new Character
        {
            Name = Names[Random.Next(Names.Length)],    // Случайное имя из массива Names
            Age = Random.Next(16, 60),                  // Случайный возраст от 16 до 60 лет
            Alive = true,                         // Персонаж жив
            Profession = ProfessionGenerator.GetRandom() // Случайная профессия из массива Professions
        };
    }
    //  if(Age >= 16 && Actionge <= 25)
    //         {
    //             Profession = "Студент"; // Профессия для возраста
    //     };
}