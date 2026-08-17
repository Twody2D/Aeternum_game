using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Хроника мира: сжимает весь лог World.Events в сводку по периодам (по
// умолчанию — десятилетиям), вместо построчного вывода каждого события
public static class ChronicleSystem
{
    private static readonly (EventType Type, string Label)[] EventLabels =
    {
        (EventType.Birth, "рождений"),
        (EventType.Death, "смертей"),
        (EventType.Marriage, "свадеб"),
        (EventType.Divorce, "разводов"),
        (EventType.Migration, "переездов"),
        (EventType.CreationOfDynasty, "основано династий")
    };

    public static List<string> BuildChronicle(World world, int periodLength = 10)
    {
        var lines = new List<string>
        {
            "",
            "===== Хроника мира ====="
        };

        var periods = world.Events
            .GroupBy(e => (e.Year - 1) / periodLength)
            .OrderBy(g => g.Key);

        foreach (var period in periods)
        {
            var startYear = period.Key * periodLength + 1;
            var endYear = startYear + periodLength - 1;

            var counts = period
                .GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var parts = EventLabels
                .Where(el => counts.ContainsKey(el.Type))
                .Select(el => $"{counts[el.Type]} {el.Label}")
                .ToList();

            if (parts.Count == 0)
            {
                continue; // Тихий период — ничего примечательного не случилось
            }

            lines.Add($"Годы {startYear}-{endYear}: {string.Join(", ", parts)}");
        }

        lines.Add("");

        return lines;
    }
}
