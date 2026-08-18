using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Выдающиеся личности: прозрачные пороговые правила вместо ML — "система сама
// решает", кто заметен, но решает явными сравнениями с порогом, а не чёрным ящиком
public static class NotablePeopleSystem
{
    private const int OldAgeThreshold = 80; // Возраст, начиная с которого считаем персонажа долгожителем
    private const int FoundedDynastyMinMembers = 15; // Сколько представителей нужно династии, чтобы считаться значимой

    public static List<NotablePerson> BuildReport(World world)
    {
        return world.Characters
            .Select(c => BuildEntry(c, world))
            .Where(n => n.IsLongLived || n.FoundedSignificantDynasty != null)
            .OrderBy(n => n.Character.BirthYear)
            .ToList();
    }

    private static NotablePerson BuildEntry(Character character, World world)
    {
        var isLongLived = character.Age >= OldAgeThreshold;

        var foundedDynasty = world.Dynasties.FirstOrDefault(d => d.Founder == character);

        // "Представителей", не "потомков" — в Dynasty.Members входят и вошедшие в род браком, не только кровные потомки
        var significant = foundedDynasty != null && foundedDynasty.Members.Count >= FoundedDynastyMinMembers
            ? foundedDynasty
            : null;

        return new NotablePerson(character, isLongLived, significant);
    }
}
