namespace Aeternum.WorldGen.Models;

public class Family
{
    // Уникальный номер семьи
    public int Id { get; set; }
    public Character? Father { get; set; }

    public Character? Mother { get; set; }


    public List<Character> Children { get; set; } = new();
}