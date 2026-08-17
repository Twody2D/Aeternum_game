
namespace Aeternum.WorldGen.Models;

// Родовой дом: объединяет всех потомков и семьи, ведущие фамилию основателя
public class Dynasty
{
    public string Name { get; set; } = ""; // Название дома ("Дом {Фамилия}")

    public List<Character> Members { get; set; } = new(); // Все персонажи династии (по крови и по браку)

    public List<Family> Families { get; set; } = new(); // Все семьи, относящиеся к этой династии
}
