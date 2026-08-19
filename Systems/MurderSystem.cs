using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Заговоры против правителя: если у правящей династии есть другой живой
// взрослый представитель, помимо самого правителя, — настоящий потенциальный
// соперник за трон — с небольшим шансом в год правитель гибнет от заговора.
// Кто унаследует трон дальше — не решается здесь: обычная логика KingdomSystem
// подберёт старшего живого члена династии в следующем году, заговорщику это
// не гарантировано
public static class MurderSystem
{
    private static readonly Random _random = new();

    private const int BraveWeight = 2; // Смелые соперники вдвое чаще решаются на заговор
    private const int GrudgeWeight = 2; // Застарелая обида на правителя (см. Character.Enemies) толкает на заговор не хуже смелости
    private const int DefaultWeight = 1;

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null || !kingdom.Ruler.Alive)
            {
                continue;
            }

            var rivals = kingdom.Dynasty.Members
                .Where(m => m.Alive && m != kingdom.Ruler && m.Age >= world.Settings.AdultAge)
                .ToList();

            if (rivals.Count == 0)
            {
                continue; // Соперничать некому — заговору не из чего родиться
            }

            if (_random.NextDouble() >= world.Settings.RegicideChance)
            {
                continue;
            }

            var rival = PickWeightedRival(rivals, kingdom.Ruler);
            var ruler = kingdom.Ruler;

            DeathSystem.Kill(ruler, world, DeathReason.Murder);

            // Заговорщик наживает врагов в лице тех, кто потерял правителя-родственника
            foreach (var heir in GetBereaved(ruler))
            {
                AddEnmity(rival, heir);
            }

            // Label-формат ("{Королевство}: ...") вместо склонения названия
            // государства — та же договорённость, что у DisasterSystem/WarSystem
            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Murder,
                Description = $"{kingdom.Name}: {SurnameSystem.GetDisplayFullName(ruler)} убит(а) в результате заговора. " +
                              $"Подозревают {SurnameSystem.GetDisplayFullName(rival)}"
            });
        }
    }

    // Живой супруг и живые дети покойного правителя — те, у кого теперь есть мотив мести
    private static IEnumerable<Character> GetBereaved(Character ruler)
    {
        if (ruler.CurrentFamily is { } family)
        {
            var spouse = family.Father == ruler ? family.Mother : family.Father;

            if (spouse.Alive)
            {
                yield return spouse;
            }

            foreach (var child in family.Children.Where(c => c.Alive))
            {
                yield return child;
            }
        }
    }

    // Взвешенный выбор соперника: смелые (см. Trait.Brave) и затаившие обиду на
    // правителя (см. Character.Enemies) чаще решаются на заговор
    private static Character PickWeightedRival(List<Character> rivals, Character ruler)
    {
        var expanded = rivals
            .SelectMany(r => Enumerable.Repeat(r, GetWeight(r, ruler)))
            .ToList();

        return expanded[_random.Next(expanded.Count)];
    }

    private static int GetWeight(Character rival, Character ruler)
    {
        var weight = DefaultWeight;

        if (rival.Traits.Contains(Trait.Brave))
        {
            weight += BraveWeight - DefaultWeight;
        }

        if (rival.Enemies.Contains(ruler))
        {
            weight += GrudgeWeight - DefaultWeight;
        }

        return weight;
    }

    // Симметричное добавление вражды — переиспользуется KingdomSystem при кризисе наследования
    public static void AddEnmity(Character a, Character b)
    {
        if (a == b)
        {
            return;
        }

        a.Enemies.Add(b);
        b.Enemies.Add(a);
    }
}
