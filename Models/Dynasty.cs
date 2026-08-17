
namespace Aeternum.WorldGen.Models;

// Родовой дом: объединяет всех потомков и семьи, ведущие фамилию основателя
public class Dynasty
{
    public int Id { get; set; } // Уникальный номер династии

    public string Name { get; set; } = ""; // Название дома ("Дом {Фамилия}")

    public List<Character> Members { get; set; } = new(); // Все персонажи династии (по крови и по браку)

    public List<Family> Families { get; set; } = new(); // Все семьи, относящиеся к этой династии

    public Character Founder { get; set; } = null!; // Кто основал династию

    public int FoundedYear { get; set; } // Год основания
}
