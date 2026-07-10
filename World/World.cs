using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.World;

public class World
{
    public int CurrentYear { get; set; }

    public List<Character> Characters { get; set; } = [];
}