using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Generators;


// Фабрика персонажей: собирает случайного жителя из хардкод-пулов имён/фамилий
public static class CharacterGenerator
{
    private static readonly Random Random = new(); // Генератор случайных чисел
    private static int _nextId = 1; // Сквозной счётчик для Character.Id

    private static readonly string[] MaleNames =
    {
        "Артур",
        "Вильгельм",
        "Альфред",
        "Эдмунд",
        "Годрик",
        "Роберт",
        "Павел",
        "Александр",
        "Дмитрий",
        "Игорь",
        "Николай",
        "Степан",
        "Фёдор",
        "Ярослав",
        "Всеволод",
        "Мстислав",
        "Владимир",
        "Богдан",
        "Тимофей",
        "Гаврила",
        "Матвей",
        "Севастьян",
        "Ратибор",
        "Ростислав",
        "Ждан"
    };

    private static readonly string[] FemaleNames =
    {
        "Елена",
        "Анна",
        "Екатерина",
        "Анастасия",
        "Алёна",
        "Любава",
        "Мария",
        "Ольга",
        "Светлана",
        "Дарья",
        "Варвара",
        "Аглая",
        "Василиса",
        "Злата",
        "Милана",
        "Забава",
        "Радмила",
        "Пелагея",
        "Агафья",
        "Марфа",
        "Ксения",
        "Ярослава",
        "Веселина",
        "Лада",
        "Зоя"
    };

    private static readonly string[] LastNames =
    {
        "Захаров",
        "Борисов",
        "Кузнецов",
        "Иванов",
        "Кулачкин",
        "Прохоров",
        "Пастухов",
        "Соколов",
        "Волков",
        "Медведев",
        "Морозов",
        "Орлов",
        "Лебедев",
        "Козлов",
        "Новиков",
        "Соловьёв",
        "Егоров",
        "Виноградов",
        "Крылов",
        "Голубев",
        "Никитин",
        "Комаров",
        "Воронов",
        "Гусев",
        "Тихонов"
    };
    
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
    // culture смещает выбор профессии в сторону предпочитаемой этим народом категории
    public static Character Create(Culture? culture = null)
    {

        var character = CreateBaseCharacter();

        character.Age = Random.Next(16, 60);
        character.Profession = ProfessionSystem.GetRandom(culture);

        return character;

    }

    // Создаёт младенца (возраст 0) для BirthSystem — родителей/фамилию проставляет вызывающий код
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

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }
}