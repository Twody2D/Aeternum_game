using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Данные для карточек крупнейших династий и списка государств
public static class DynastyEncyclopediaSystem
{
    private const int TopDynastiesCount = 5;
    private const int LongLivedAge = 80; // Тот же порог, что и у NotablePeopleSystem
    private const int LongLivedDisplayLimit = 3;

    public static List<DynastyStat> BuildReport(World world, int topN = TopDynastiesCount)
    {
        return world.Dynasties
            .OrderByDescending(d => d.Members.Count)
            .Take(topN)
            .Select(BuildStat)
            .ToList();
    }

    private static DynastyStat BuildStat(Dynasty dynasty)
    {
        var aliveCount = dynasty.Members.Count(m => m.Alive);

        int? extinctYear = aliveCount == 0
            ? dynasty.Members.Max(m => m.DeathYear ?? dynasty.FoundedYear)
            : null;

        var longLived = dynasty.Members
            .Where(m => m.Age >= LongLivedAge)
            .OrderByDescending(m => m.Age)
            .Take(LongLivedDisplayLimit)
            .ToList();

        return new DynastyStat(dynasty, aliveCount, extinctYear, longLived);
    }

    // Список государств, отсортированный по числу подконтрольных поселений —
    // у Kingdom уже есть все нужные поля (FallenYear, Ruler, FoundedYear)
    public static List<Kingdom> BuildKingdomsReport(World world)
    {
        return world.Kingdoms
            .OrderByDescending(k => k.Settlements.Count)
            .ToList();
    }
}
