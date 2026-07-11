
namespace Aeternum.WorldGen.Models;
public class Dynasty
{
    public string Name { get; set; } = "";

    public List<Character> Members { get; set; } = new();

    public List<Family> Families { get; set; } = new();
}