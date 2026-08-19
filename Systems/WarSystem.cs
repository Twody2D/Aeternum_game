using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Войны между государствами — без отдельной сущности "армия", гарнизон
// поселения определяется прямо по живущим в нём профессиям Military. Казус
// белли уже есть в данных: спорное поселение — то, что попадает в
// Kingdom.Settlements сразу у двух и более королевств одновременно (порог
// контроля в KingdomSystem — "заметное присутствие", не большинство, поэтому
// пересечения реальны). Однажды начавшись, война становится осадой — длится
// подряд несколько лет с растущими потерями, пока спор не разрешится сам собой
// через обычный ежегодный пересчёт территориального контроля (KingdomSystem)
public static class WarSystem
{
    private static readonly Random _random = new();

    private const double EscalationPerYear = 0.15; // Затяжная осада изматывает сильнее внезапного набега
    private const int MaxEscalationYears = 5; // Максимум +75% к потерям на 5+ году осады

    private const double DefenseBonusPerDefender = 0.05; // Гарнизон (профессии Military) снижает потери
    private const double MaxDefenseBonus = 0.3;

    private const double HolyWarChanceMultiplier = 1.5; // Спор на почве веры легче перерастает в открытую войну
    private const double HolyWarCasualtyMultiplier = 1.3; // ...и идёт кровопролитнее

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var claimants = world.Kingdoms
                .Where(k => k.Settlements.Contains(settlement))
                .ToList();

            if (claimants.Count < 2 || AreAllAllied(claimants))
            {
                settlement.SiegeYears = 0; // Спор разрешился или снят — осада прекращается
                continue;
            }

            var isHolyWar = IsReligiousDispute(claimants);
            var effectiveWarChance = isHolyWar ? world.Settings.WarChance * HolyWarChanceMultiplier : world.Settings.WarChance;

            // Осада ещё не началась — как и раньше, решает WarChance. Уже идущая
            // осада не бросает эту монету заново — раз начавшись, не может
            // случайно "передумать" и продолжается, пока не разрешится исходом
            if (settlement.SiegeYears == 0 && _random.NextDouble() >= effectiveWarChance)
            {
                continue;
            }

            settlement.SiegeYears++;

            DeclareWar(settlement, claimants, world, isHolyWar);
        }
    }

    // Спор религиозный, если у претендентов есть хотя бы два разных вероисповедания
    // правящего дома — тот же приём, что AllianceSystem использует для союзов
    private static bool IsReligiousDispute(List<Kingdom> claimants)
    {
        return claimants
            .Select(k => k.Ruler.Settlement?.Religion)
            .Where(r => r != null)
            .Distinct()
            .Count() > 1;
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

    private static void DeclareWar(Settlement settlement, List<Kingdom> claimants, World world, bool isHolyWar)
    {
        var residents = settlement.Members.Where(m => m.Alive).ToList();

        var escalation = 1 + EscalationPerYear * Math.Min(settlement.SiegeYears, MaxEscalationYears);

        var defenders = residents.Count(m => ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military);
        var defenseFactor = 1 - Math.Min(MaxDefenseBonus, defenders * DefenseBonusPerDefender);

        var effectiveCasualtyRate = world.Settings.WarCasualtyRate * escalation * defenseFactor * WallSystem.GetWallFactor(settlement);

        if (isHolyWar)
        {
            effectiveCasualtyRate *= HolyWarCasualtyMultiplier;
        }

        var casualtyCount = (int)(residents.Count * effectiveCasualtyRate);

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
        var holyWarNote = isHolyWar ? " Война на почве веры." : "";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.War,
            Description = $"{settlement.Name}: {settlement.SiegeYears}-й год осады. Претенденты: {claimantNames}. Погибших: {casualties.Count}.{holyWarNote}"
        });
    }
}
