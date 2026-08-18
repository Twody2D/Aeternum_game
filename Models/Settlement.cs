namespace Aeternum.WorldGen.Models;

// Населённый пункт: локальная экономика и население. Браки заключаются
// только внутри одного поселения (см. MarriageSystem), еда производится
// и потребляется локально (см. EconomySystem) — поселения независимы друг от друга
public class Settlement
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double FoodStock { get; set; } // Локальный запас еды (аналог World.FoodStock, но на одно поселение)

    public double MaterialStock { get; set; } // Запас материалов (ремесленное производство) — пока только копится, тратить пока не на что

    public List<Character> Members { get; set; } = new(); // Все, кто когда-либо жил здесь, включая умерших

    public Culture? Culture { get; set; } // Культура поселения — влияет на распределение профессий жителей

    public Religion? Religion { get; set; } // Религия поселения — влияет на шанс межпоселенческого брака
}
