using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.GameWorld;

public class World
{
    public int CurrentYear { get; set; } //Текущий год

    public List<Character> Characters { get; set; } = []; //Список жителей
}