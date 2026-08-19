namespace Aeternum.WorldGen.Models;

// Культура поселения. Пока не претендует на полноценную систему (язык,
// традиции, религия) — единственный реальный эффект: при случайном выборе
// профессии для жителя чуть выше шанс получить профессию из PreferredCategory
// (см. ProfessionSystem.GetRandom)
public class Culture
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public ProfessionCategory PreferredCategory { get; set; }

    public SuccessionLaw SuccessionLaw { get; set; } // Обычай наследования престола, см. SuccessionSystem
}
