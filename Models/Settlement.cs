namespace Aeternum.WorldGen.Models;

// Населённый пункт: локальная экономика и население. Браки заключаются
// только внутри одного поселения (см. MarriageSystem), еда производится
// и потребляется локально (см. EconomySystem) — поселения независимы друг от друга
public class Settlement
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double FoodStock { get; set; } // Локальный запас еды (аналог World.FoodStock, но на одно поселение)

    public Dictionary<MaterialType, double> MaterialStocks { get; set; } = new(); // Запасы материалов по типам (ремесленное производство), см. MaterialType

    public int Houses { get; set; } // Число домов — растут вместе с населением, см. HousingSystem

    public int Hospitals { get; set; } // Число больниц — снижают детскую смертность и тяжесть эпидемий, см. HospitalSystem

    public Dictionary<MaterialType, int> Workshops { get; set; } = new(); // Мастерские по типу материала — усиливают его производство, см. WorkshopSystem

    public int Schools { get; set; } // Число школ — повышают шанс профессии категории Knowledge при взрослении, см. SchoolSystem/ProfessionSystem

    public int Walls { get; set; } // Число укреплений — снижают потери при войне за поселение, см. WallSystem

    public double Gold { get; set; } // Золото — производится профессиями категории Trade, см. ProfessionSystem.GetGoldProduction

    public double X { get; set; } // Координаты на условной карте — см. SettlementGenerator
    public double Y { get; set; }

    public int SiegeYears { get; set; } // Сколько подряд лет поселение находится в осаде, см. WarSystem
    public int TruceUntilYear { get; set; } // Год, до которого действует перемирие после затянувшейся осады (0 — перемирия нет), см. WarSystem

    public List<Character> Members { get; set; } = new(); // Все, кто когда-либо жил здесь, включая умерших

    public Culture? Culture { get; set; } // Культура поселения — влияет на распределение профессий жителей

    public Religion? Religion { get; set; } // Религия поселения — влияет на шанс межпоселенческого брака

    public int LegendCount { get; set; } // Число легендарных долгожителей, доживших здесь свой век — см. DeathSystem, MigrationSystem
}
