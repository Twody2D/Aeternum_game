namespace Aeternum.WorldGen.Models;

// DTO итоговых отчётов: BuildX-методы отчётных систем (StatisticsSystem,
// ChronicleSystem, NotablePeopleSystem, DynastyEncyclopediaSystem) возвращают
// эти records вместо готового русского текста — ссылки на настоящие
// Character/Dynasty/Kingdom/Settlement, а не на их текстовые имена. Русский
// текст строит только консольный вывод в Program.cs — то же место, где уже
// печатается погодовой лог событий
public record WorldStatistics(
    int CurrentYear,
    int TotalCharactersCreated,
    int AliveCount,
    int TotalBirths,
    int TotalDeaths,
    List<SettlementStat> Settlements,
    List<AgeGroupCount> AgeGroups);

public record SettlementStat(Settlement Settlement, int Population);

public record AgeGroupCount(AgeGroup Group, int Count);

public record ChroniclePeriod(int StartYear, int EndYear, List<EventTally> Tallies);

public record EventTally(EventType Type, int Count);

public record NotablePerson(Character Character, bool IsLongLived, Dynasty? FoundedSignificantDynasty);

public record DynastyStat(Dynasty Dynasty, int AliveCount, int? ExtinctYear, List<Character> LongLived);
