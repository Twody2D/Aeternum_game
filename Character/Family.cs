namespace Aeternum.WorldGen.Characters;

public class Family
{
    public Character? Father { get; set; }

    public Character? Mother { get; set; }


    public List<Character> Children { get; set; } = new();
}