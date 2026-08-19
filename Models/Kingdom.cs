namespace Aeternum.WorldGen.Models;

// Государство: возникает, когда одна династия становится доминирующей
// в нескольких поселениях (см. KingdomSystem). Без карты/географии — "территория"
// здесь это просто список Settlement, контроль над которыми пересчитывается каждый год
public class Kingdom
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public Dynasty Dynasty { get; set; } = null!; // Правящая династия

    public Character Ruler { get; set; } = null!; // Старший живой член правящей династии

    public int FoundedYear { get; set; }

    public int? FallenYear { get; set; } // Год, когда правящая династия полностью угасла; null — государство существует

    public List<Settlement> Settlements { get; set; } = new(); // Контролируемые поселения на текущий год

    public List<Kingdom> AlliedKingdoms { get; set; } = new(); // Союзные государства (см. AllianceSystem) — симметрично, есть у обеих сторон

    public double FoodTreasury { get; set; } // Казна еды — собирается данью с подконтрольных поселений (см. TributeSystem)

    public Dictionary<MaterialType, double> MaterialTreasury { get; set; } = new(); // Казна материалов по типам — то же для материалов
}
