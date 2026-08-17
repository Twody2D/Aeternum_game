using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Карточки крупнейших династий: основание, статус, число представителей, долгожители
public static class DynastyEncyclopediaSystem
{
    private const int TopDynastiesCount = 5;
    private const int LongLivedAge = 80; // Тот же порог, что и у NotablePeopleSystem
    private const int LongLivedDisplayLimit = 3;

    public static List<string> BuildReport(World world, int topN = TopDynastiesCount)
    {
        var lines = new List<string>
        {
            "",
            "===== Династии ====="
        };

        var dynasties = world.Dynasties
            .OrderByDescending(d => d.Members.Count)
            .Take(topN);

        foreach (var dynasty in dynasties)
        {
            lines.Add("");
            lines.Add($"--- {dynasty.Name} ---");
            lines.Add($"Основана: {dynasty.FoundedYear} год, основатель — {SurnameSystem.GetDisplayFullName(dynasty.Founder)}");

            var aliveCount = dynasty.Members.Count(m => m.Alive);

            if (aliveCount == 0)
            {
                var extinctYear = dynasty.Members.Max(m => m.DeathYear ?? dynasty.FoundedYear);
                lines.Add($"Угасла: {extinctYear} год");
            }
            else
            {
                lines.Add($"Ныне живущих представителей: {aliveCount}");
            }

            lines.Add($"Всего представителей за всю историю: {dynasty.Members.Count}");

            var longLived = dynasty.Members
                .Where(m => m.Age >= LongLivedAge)
                .OrderByDescending(m => m.Age)
                .Take(LongLivedDisplayLimit)
                .ToList();

            if (longLived.Count > 0)
            {
                var names = string.Join(", ", longLived.Select(m => $"{SurnameSystem.GetDisplayFullName(m)} ({m.Age})"));
                lines.Add($"Долгожители: {names}");
            }
        }

        lines.Add("");

        return lines;
    }
}
