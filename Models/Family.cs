namespace Aeternum.WorldGen.Models;

public class Family
{
    public Character? Father { get; set; }

    public Character? Mother { get; set; }


    public List<Character> Children { get; set; } = new();
}