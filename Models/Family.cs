namespace Aeternum.WorldGen.Models;

// Семья, образованная браком: отец + мать + их общие дети
public class Family
{
    // Уникальный номер семьи
    public int Id { get; set; }
    public Character Father { get; set; } = null!;

    public Character Mother { get; set; } = null!;

    public List<Character> Children { get; set; } = new();

    public Dynasty? Dynasty { get; set; } // Династия, к которой принадлежит семья
}
