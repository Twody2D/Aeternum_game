using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.GameWorld;
public class Dynasty
{
    public string Name { get; set; } = "";

    public List<Character> Members { get; set; } = new();

    public List<Family> Families { get; set; } = new();
}