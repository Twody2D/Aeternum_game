using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Generators;


// Фабрика персонажей: собирает случайного жителя из пулов имён/фамилий,
// загруженных из Data/Names.json (см. ContentData)
public static class CharacterGenerator
{
    private static readonly Random Random = new(); // Генератор случайных чисел
    private static int _nextId = 1; // Сквозной счётчик для Character.Id

    private static string[] MaleNames => ContentData.Names.MaleNames;
    private static string[] FemaleNames => ContentData.Names.FemaleNames;
    private static string[] LastNames => ContentData.Names.LastNames;

    // Общая часть создания персонажа: пол, имя, фамилия, уникальный Id
    private static Character CreateBaseCharacter()
    {
        var gender = GetRandomGender();

        return new Character
        {
            Id = _nextId++,
            Name = GenerateName(gender),
            LastName = GenerateLastName(),
            Gender = gender,
            Alive = true
        };
    }

    // Создаёт "взрослого с историей" — используется при генерации стартового населения.
    // culture смещает выбор профессии в сторону предпочитаемой этим народом категории;
    // settlement — чтобы гарантированно закрыть недостающие обязательные профессии
    public static Character Create(Culture? culture = null, Settlement? settlement = null)
    {

        var character = CreateBaseCharacter();

        character.Age = Random.Next(16, 60);
        character.Profession = ProfessionSystem.GetRandom(culture, settlement);
        AssignTraits(character);

        return character;

    }

    // Создаёт младенца (возраст 0) для BirthSystem — родителей/фамилию проставляет вызывающий код
    public static Character CreateNewborn()
    {
        var character = CreateBaseCharacter();

        character.Age = 0;
        character.LifeStage = LifeStage.Infant;
        AssignTraits(character);

        return character;
    }

    private const double TraitChance = 0.2; // Независимый шанс на каждую черту при рождении/создании

    // Brave и Prudent — противоположности по риску, не могут достаться одному персонажу разом
    private static void AssignTraits(Character character)
    {
        foreach (var trait in Enum.GetValues<Trait>())
        {
            if (Random.NextDouble() < TraitChance)
            {
                character.Traits.Add(trait);
            }
        }

        if (character.Traits.Contains(Trait.Brave) && character.Traits.Contains(Trait.Prudent))
        {
            character.Traits.Remove(Random.Next(2) == 0 ? Trait.Brave : Trait.Prudent);
        }
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

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}