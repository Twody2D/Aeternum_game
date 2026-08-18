using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Войны между государствами — без армий и битв как отдельной механики.
// Казус белли уже есть в данных: спорное поселение — то, что попадает в
// Kingdom.Settlements сразу у двух и более королевств одновременно (порог
// контроля в KingdomSystem — "заметное присутствие", не большинство, поэтому
// пересечения реальны). Передел территории ничего досчитывать не нужно —
// KingdomSystem и так пересчитывает контроль каждый год по числу живых жителей
public static class WarSystem
{
    private static readonly Random _random = new();

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var claimants = world.Kingdoms
                .Where(k => k.Settlements.Contains(settlement))
                .ToList();

            if (claimants.Count < 2)
            {
                continue;
            }

            if (AreAllAllied(claimants))
            {
                continue; // Союзники не воюют друг с другом за спорное поселение
            }

            if (_random.NextDouble() >= world.Settings.WarChance)
            {
                continue;
            }

            DeclareWar(settlement, claimants, world);
        }
    }

    private static bool AreAllAllied(List<Kingdom> claimants)
    {
        for (var i = 0; i < claimants.Count; i++)
        {
            for (var j = i + 1; j < claimants.Count; j++)
            {
                if (!claimants[i].AlliedKingdoms.Contains(claimants[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void DeclareWar(Settlement settlement, List<Kingdom> claimants, World world)
    {
        var residents = settlement.Members.Where(m => m.Alive).ToList();
        var casualtyCount = (int)(residents.Count * world.Settings.WarCasualtyRate);

        var casualties = residents
            .OrderBy(_ => _random.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.War);
        }

        // Именительный падеж безопасен для любого числа претендентов —
        // "между X и Y" потребовало бы творительного, которого мы не умеем
        var claimantNames = string.Join(", ", claimants.Select(k => k.Name));

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.War,
            Description = $"{settlement.Name}: спор перерос в войну. Претенденты: {claimantNames}. Погибших: {casualties.Count}"
        });
    }
}
