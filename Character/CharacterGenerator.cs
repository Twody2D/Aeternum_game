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

    private static readonly Random Random = new(); // Генератор случайных чисел

    public static Character Create()   // Метод для создания нового персонажа
    {
        return new Character
        {
            Name = Names[Random.Next(Names.Length)],    // Случайное имя из массива Names
            Age = Random.Next(16, 60),                  // Случайный возраст от 16 до 60 лет
            Alive = true                                // Персонаж жив
        };
    }
}