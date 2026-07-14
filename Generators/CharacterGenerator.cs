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

    private static readonly string[] LastNames =
    {
        "Захаров",
        "Борисов",
        "Кузнецов",
        "Иванов",
        "Кулачкин",
        "Прохоров",
        "Пастухов"
    };
    
    private static Character CreateBaseCharacter()
    {
        var gender = GetRandomGender();

        return new Character
        {
            Name = GenerateName(gender),
            LastName = GenerateLastName(),
            Gender = gender,
            Alive = true
        };
    }
    public static Character Create()   // Метод для создания нового персонажа
    {              

        var character = CreateBaseCharacter();

        character.Age = Random.Next(16, 60);
        character.Profession = ProfessionSystem.GetRandom();

        return character;
   
    }
    public static Character CreateNewborn()
    {
        var character = CreateBaseCharacter();

        character.Age = 0;
        character.LifeStage = LifeStage.Infant;

        return character;
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

    private static string GenerateLastName()
    {
        return LastNames[Random.Next(LastNames.Length)];
    }
}