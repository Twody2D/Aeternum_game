using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Generators;


public static class CharacterGenerator
{
    private static readonly Random Random = new(); // Генератор случайных чисел

    private static readonly string[] MaleNames =
    {
        "Артур",
        "Вильгельм",
        "Альфред",
        "Эдмунд",
        "Годрик",
        "Роберт",
        "Павел"
    };

    private static readonly string[] FemaleNames =
    {
        "Елена",
        "Анна",
        "Екатерина",
        "Анастасия",
        "Алёна",
        "Любава",
        "Мария"
    };
    
    public static Character Create()   // Метод для создания нового персонажа
    {              
        var gender = GetRandomGender();
        return new Character
        {
            Name = GenerateName(gender),    // Случайное имя из массива Names
            Age = Random.Next(16, 60),                  // Случайный возраст от 16 до 60 лет
            Gender = gender,                  // Случайный пол персонажа
            Alive = true,                         // Персонаж жив
            Profession = ProfessionSystem.GetRandom() // Случайная профессия из массива Professions
        };
    }
    public static Character CreateNewborn()
    {
        var gender = GetRandomGender();
        return new Character
        
        {
            Name = GenerateName(gender),    // Случайное имя из массива Names
            Age = 0,                  // Возраст новорожденного персонажа устанавливается в 0
            Gender = gender,                  // Случайный пол персонажа
            Alive = true,                         // Персонаж жив
            LifeStage = LifeStage.Infant          // Этап жизни для новорожденного
        };
    }
    private static Gender GetRandomGender()
    {
        return Random.Next(2) == 0 ? Gender.Male : Gender.Female; // Случайный выбор пола персонажа (мужской или женский)
    }

    private static string GenerateName(Gender gender)
{
    return gender switch
    {
        Gender.Male =>
            MaleNames[Random.Next(MaleNames.Length)],

        Gender.Female =>
            FemaleNames[Random.Next(FemaleNames.Length)],

        _ => "Безымянный"
    };
}
}