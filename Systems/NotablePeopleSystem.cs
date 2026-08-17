using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Выдающиеся личности: прозрачные пороговые правила вместо ML — "система сама
// решает", кто заметен, но решает явными сравнениями с порогом, а не чёрным ящиком
public static class NotablePeopleSystem
{
    private const int OldAgeThreshold = 80; // Возраст, начиная с которого считаем персонажа долгожителем
    private const int FoundedDynastyMinMembers = 15; // Сколько потомков нужно династии, чтобы считаться значимой

    public static List<string> BuildReport(World world)
    {
        var lines = new List<string>
        {
            "",
            "===== Выдающиеся личности ====="
        };

        var notable = world.Characters
            .Select(c => (Character: c, Reasons: GetReasons(c, world)))
            .Where(n => n.Reasons.Count > 0)
            .OrderBy(n => n.Character.BirthYear)
            .ToList();

        if (notable.Count == 0)
        {
            lines.Add("В этой истории не нашлось никого выдающегося.");
        }

        foreach (var (character, reasons) in notable)
        {
            lines.Add($"{SurnameSystem.GetDisplayFullName(character)} — {string.Join(", ", reasons)}");
        }

        lines.Add("");

        return lines;
    }

    private static List<string> GetReasons(Character character, World world)
    {
        var reasons = new List<string>();

        if (character.Age >= OldAgeThreshold)
        {
            reasons.Add($"дожил(а) до {character.Age} лет");
        }

        var foundedDynasty = world.Dynasties.FirstOrDefault(d => d.Founder == character);
        if (foundedDynasty != null && foundedDynasty.Members.Count >= FoundedDynastyMinMembers)
        {
            reasons.Add($"основал(а) {foundedDynasty.Name} ({foundedDynasty.Members.Count} потомков)");
        }

        return reasons;
    }
}
