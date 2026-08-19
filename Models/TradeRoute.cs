namespace Aeternum.WorldGen.Models;

// Постоянная торговая связь между двумя поселениями — крепнет год от года при
// повторной торговле (см. TradeSystem) и делает будущий обмен между ними
// эффективнее. Ненаправленная пара, как Character.Enemies
public class TradeRoute
{
    public Settlement A { get; set; } = null!;
    public Settlement B { get; set; } = null!;

    public int Years { get; set; } // Сколько лет пара уже торгует друг с другом — никогда не убывает
}
