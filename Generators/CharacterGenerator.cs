using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Generators;


public static class CharacterGenerator
{
    private static readonly string[] Names =
    {
        "Артур",
        "Вильгельм",
        "Альфред",
        "Эдмунд",
        "Годрик",
        "Роберт",
        "Елена"
    };

    private static readonly Random Random = new(); // Генератор случайных чисел

    public static Character Create()   // Метод для создания нового персонажа
    {              
        
        return new Character
        {
            Name = Names[Random.Next(Names.Length)],    // Случайное имя из массива Names
            Age = Random.Next(16, 60),                  // Случайный возраст от 16 до 60 лет
            Gender = GetRandomGender(),                  // Случайный пол персонажа
            Alive = true,                         // Персонаж жив
            Profession = ProfessionSystem.GetRandom() // Случайная профессия из массива Professions
        };
    }
    public static Character CreateNewborn()
    {
        return new Character
        {
            Name = Names[Random.Next(Names.Length)],    // Случайное имя из массива Names
            Age = 0,                  // Возраст новорожденного персонажа устанавливается в 0
            Gender = GetRandomGender(),                  // Случайный пол персонажа
            Alive = true,                         // Персонаж жив
            LifeStage = LifeStage.Infant          // Этап жизни для новорожденного
        };
    }
    private static Gender GetRandomGender()
    {
        return Random.Next(2) == 0 ? Gender.Male : Gender.Female; // Случайный выбор пола персонажа (мужской или женский)
    }
}