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

    private static readonly Random Random = new();

    public static Character Create()
    {
        return new Character
        {
            Name = Names[Random.Next(Names.Length)],
            Age = Random.Next(16, 60),
            Alive = true
        };
    }
}