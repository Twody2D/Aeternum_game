using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Save;

// Плоское представление World для сериализации: вместо прямых ссылок на объекты
// (которые образуют циклы — Character.Mother/Family, Family.Father/Children и т.д.)
// персонажи/семьи/династии ссылаются друг на друга по Id. SaveSystem превращает
// это обратно в живой объектный граф при загрузке
public class SaveData
{
    public int CurrentYear { get; set; }
    public int TotalBirths { get; set; }
    public int TotalDeaths { get; set; }
    public int AliveCount { get; set; }
    public double Knowledge { get; set; }
    public int Seed { get; set; }

    public WorldSettings Settings { get; set; } = new();

    public List<CharacterData> Characters { get; set; } = new();
    public List<FamilyData> Families { get; set; } = new();
    public List<DynastyData> Dynasties { get; set; } = new();
    public List<SettlementData> Settlements { get; set; } = new();
    public List<CultureData> Cultures { get; set; } = new();
    public List<ReligionData> Religions { get; set; } = new();
    public List<KingdomData> Kingdoms { get; set; } = new();
    public List<TradeRouteData> TradeRoutes { get; set; } = new();

    // У WorldEvent нет ссылок на объекты — сохраняется как есть, без DTO-обёртки
    public List<WorldEvent> Events { get; set; } = new();
}

public class CharacterData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
    public string? Profession { get; set; }
    public int ProfessionYear { get; set; }
    public Gender Gender { get; set; }
    public bool Alive { get; set; }
    public DeathReason DeathReason { get; set; }
    public LifeStage LifeStage { get; set; }
    public int BirthYear { get; set; }
    public int? DeathYear { get; set; }

    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public int? GuardianId { get; set; }
    public int? BirthFamilyId { get; set; }
    public int? CurrentFamilyId { get; set; }
    public int? DynastyId { get; set; }
    public int? SettlementId { get; set; }

    public List<Trait> Traits { get; set; } = new();
    public List<int> EnemyIds { get; set; } = new();
    public List<int> FriendIds { get; set; } = new();
}

public class FamilyData
{
    public int Id { get; set; }
    public int FatherId { get; set; }
    public int MotherId { get; set; }
    public List<int> ChildrenIds { get; set; } = new();
    public int? DynastyId { get; set; }
    public int FormedYear { get; set; }
}

public class DynastyData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<int> MemberIds { get; set; } = new();
    public List<int> FamilyIds { get; set; } = new();
    public int FounderId { get; set; }
    public int FoundedYear { get; set; }
    public double Reputation { get; set; }
}

public class SettlementData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double FoodStock { get; set; }
    public Dictionary<MaterialType, double> MaterialStocks { get; set; } = new();
    public int Houses { get; set; }
    public int Hospitals { get; set; }
    public Dictionary<MaterialType, int> Workshops { get; set; } = new();
    public int Schools { get; set; }
    public int Walls { get; set; }
    public double Gold { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int SiegeYears { get; set; }
    public int TruceUntilYear { get; set; }
    public int RebellingUntilYear { get; set; }
    public int? RebellingAgainstKingdomId { get; set; }
    public List<int> MemberIds { get; set; } = new();
    public int? CultureId { get; set; }
    public int? ReligionId { get; set; }
    public int LegendCount { get; set; }
}

public class CultureData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ProfessionCategory PreferredCategory { get; set; }
    public SuccessionLaw SuccessionLaw { get; set; }
}

public class ReligionData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class KingdomData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DynastyId { get; set; }
    public int RulerId { get; set; }
    public int FoundedYear { get; set; }
    public int? FallenYear { get; set; }
    public List<int> SettlementIds { get; set; } = new();
    public List<int> AlliedKingdomIds { get; set; } = new();
    public int? SuzerainId { get; set; }
    public double FoodTreasury { get; set; }
    public Dictionary<MaterialType, double> MaterialTreasury { get; set; } = new();
    public double GoldTreasury { get; set; }
    public double TributeRate { get; set; }
    public Dictionary<CourtOffice, int> CourtIds { get; set; } = new();
}

public class TradeRouteData
{
    public int SettlementAId { get; set; }
    public int SettlementBId { get; set; }
    public int Years { get; set; }
}
