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

    private const int TruceThresholdYears = 5; // Осада, длящаяся столько лет, может закончиться перемирием
    private const double TruceChance = 0.2; // Шанс в год, что затянувшаяся осада сменится перемирием
    private const int TruceDuration = 10; // На сколько лет спор замирает, даже если формально не решён

    private const int VassalizationThresholdYears = 3; // Осада, длящаяся столько лет, может закончиться вассалитетом слабой стороны
    private const double VassalizationPowerRatio = 2.0; // Во сколько раз сильная сторона должна превосходить слабую по населению
    private const double VassalizationChance = 0.15; // Шанс в год, что явно проигрышная позиция обернётся вассалитетом

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            if (world.CurrentYear < settlement.TruceUntilYear)
            {
                continue; // Стороны устали воевать — спор поставлен на паузу, даже если претензии сохраняются
            }

            var claimants = world.Kingdoms
                .Where(k => k.Settlements.Contains(settlement))
                .ToList();

            if (claimants.Count < 2 || IsPeaceful(claimants))
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

            if (claimants.Count == 2 && settlement.SiegeYears >= VassalizationThresholdYears &&
                TryGetLopsidedPair(claimants[0], claimants[1], out var stronger, out var weaker) &&
                _random.NextDouble() < VassalizationChance)
            {
                DeclareVassalization(settlement, weaker, stronger, world);
                continue;
            }

            if (settlement.SiegeYears >= TruceThresholdYears && _random.NextDouble() < TruceChance)
            {
                DeclareTruce(settlement, claimants, world);
                continue;
            }

            settlement.SiegeYears++;

            DeclareWar(settlement, claimants, world, isHolyWar);
        }
    }

    // Явный перевес сил — сильная сторона хотя бы втрое многолюднее слабой
    private static bool TryGetLopsidedPair(Kingdom a, Kingdom b, out Kingdom stronger, out Kingdom weaker)
    {
        var powerA = GetPower(a);
        var powerB = GetPower(b);

        stronger = powerA >= powerB ? a : b;
        weaker = stronger == a ? b : a;

        return GetPower(stronger) >= GetPower(weaker) * VassalizationPowerRatio;
    }

    // Сила государства — живое население всех подконтрольных поселений
    private static int GetPower(Kingdom kingdom)
    {
        return kingdom.Settlements.SelectMany(s => s.Members).Count(m => m.Alive);
    }

    // Явно проигрышная позиция решается политически быстрее, чем истощает
    // обе стороны поровну (см. DeclareTruce) — слабая сторона признаёт
    // вассалитет сильной вместо продолжения бессмысленного сопротивления
    private static void DeclareVassalization(Settlement settlement, Kingdom weaker, Kingdom stronger, World world)
    {
        weaker.Suzerain = stronger;
        settlement.SiegeYears = 0;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Vassalization,
            Description = $"{settlement.Name}: {weaker.Name} признало вассалитет {stronger.Name} вместо продолжения войны"
        });
    }

    // Затянувшаяся осада изматывает обе стороны — перемирие не решает спор, но
    // останавливает бои на TruceDuration лет (см. Settlement.TruceUntilYear)
    private static void DeclareTruce(Settlement settlement, List<Kingdom> claimants, World world)
    {
        settlement.SiegeYears = 0;
        settlement.TruceUntilYear = world.CurrentYear + TruceDuration;

        var claimantNames = string.Join(", ", claimants.Select(k => k.Name));

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Peace,
            Description = $"{settlement.Name}: затянувшаяся осада закончилась перемирием. Стороны: {claimantNames}"
        });
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

    // Претенденты не воюют друг с другом, если все пары либо союзны, либо уже
    // состоят в отношении сюзерен-вассал (см. DeclareVassalization) — второе
    // заодно исключает зацикливание: раз отношение установлено, вассалитет
    // для этой пары больше не рассматривается заново
    private static bool IsPeaceful(List<Kingdom> claimants)
    {
        for (var i = 0; i < claimants.Count; i++)
        {
            for (var j = i + 1; j < claimants.Count; j++)
            {
                if (!AreAtPeace(claimants[i], claimants[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool AreAtPeace(Kingdom a, Kingdom b)
    {
        return a.AlliedKingdoms.Contains(b) || a.Suzerain == b || b.Suzerain == a;
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
