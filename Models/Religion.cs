namespace Aeternum.WorldGen.Models;

// Религия поселения. По образцу Culture — лёгкая сущность с одним реальным
// эффектом: при разных религиях жениха и невесты снижается шанс межпоселенческого
// брака (см. MarriageSystem.DifferentReligionPenalty)
public class Religion
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}
