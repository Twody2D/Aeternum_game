namespace Aeternum.WorldGen.Models;

// Населённый пункт: локальная экономика и население. Браки заключаются
// только внутри одного поселения (см. MarriageSystem), еда производится
// и потребляется локально (см. EconomySystem) — поселения независимы друг от друга
public class Settlement
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double FoodStock { get; set; } // Локальный запас еды (аналог World.FoodStock, но на одно поселение)

    public List<Character> Members { get; set; } = new(); // Все, кто когда-либо жил здесь, включая умерших
}
