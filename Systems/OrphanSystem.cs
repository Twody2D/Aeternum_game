using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Опекунство: если у ребёнка умерли оба родителя (или умер прежний опекун),
// поселение подбирает нового — сперва среди кровной родни (та же династия),
// иначе среди любых взрослых поселения (община). Если некому — ребёнок
// остаётся без опеки, что даёт небольшой риск смерти от безнадзорности
public static class OrphanSystem
{
    private static readonly Random _random = new();

    private const double NeglectChance = 0.05; // Шанс смерти ребёнка без опекуна за год

    public static void Process(World world)
    {
        var needsGuardian = world.Characters.Where(c =>
            c.Alive &&
            c.LifeStage is LifeStage.Infant or LifeStage.Child or LifeStage.Student &&
            (c.Mother == null || !c.Mother.Alive) &&
            (c.Father == null || !c.Father.Alive) &&
            (c.Guardian == null || !c.Guardian.Alive));

        foreach (var orphan in needsGuardian.ToList())
        {
            var guardian = FindGuardian(orphan);

            if (guardian == null)
            {
                if (_random.NextDouble() < NeglectChance)
                {
                    DeathSystem.Kill(orphan, world, DeathReason.Neglect);
                }

                continue;
            }

            orphan.Guardian = guardian;
            orphan.Friends.Add(guardian);
            guardian.Friends.Add(orphan);

            var tookVerb = guardian.Gender == Gender.Female ? "взяла" : "взял";

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Guardianship,
                Description = $"{SurnameSystem.GetDisplayFullName(guardian)} {tookVerb} под опеку {SurnameSystem.GetDisplayFullName(orphan)}"
            });
        }
    }

    private static Character? FindGuardian(Character orphan)
    {
        var settlement = orphan.Settlement;

        if (settlement == null)
        {
            return null;
        }

        var candidates = settlement.Members
            .Where(m => m.Alive && m != orphan && m.LifeStage is LifeStage.Adult or LifeStage.Elder)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        // Кровная родня (та же династия) в приоритете, иначе — любой взрослый общины
        var relatives = orphan.Dynasty == null
            ? new List<Character>()
            : candidates.Where(m => m.Dynasty == orphan.Dynasty).ToList();

        var pool = relatives.Count > 0 ? relatives : candidates;

        return pool[_random.Next(pool.Count)];
    }
}
