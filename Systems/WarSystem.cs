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
    private const double EscalationPerYear = 0.15; // Затяжная осада изматывает сильнее внезапного набега
    private const int MaxEscalationYears = 5; // Максимум +75% к потерям на 5+ году осады

    private const double DefenseBonusPerDefender = 0.05; // Гарнизон (профессии Military) снижает потери
    private const double MaxDefenseBonus = 0.3;

    private const double HolyWarChanceMultiplier = 1.5; // Спор на почве веры легче перерастает в открытую войну
    private const double HolyWarCasualtyMultiplier = 1.3; // ...и идёт кровопролитнее

    private const int TruceThresholdYears = 5; // Осада, длящаяся столько лет, может закончиться перемирием
    private const double TruceChance = 0.2; // Шанс в год, что затянувшаяся осада сменится перемирием
    private const int TruceDuration = 10; // На сколько лет спор замирает, даже если формально не решён

    private const double SiegeWallDamageChance = 0.3; // Шанс, что за год осады рухнет одно укрепление

    // Насколько вероятнее прочих погибнуть на войне. Первыми стоят те, кто
    // держит оборону; за ними — мужчины призывного возраста, которых сгоняют
    // на стены ополчением; женщин и стариков задевает уже случайностью осады
    // (обстрел, штурм, голод), а до детей война добирается в последнюю очередь.
    // Веса относительные: важно не само число, а во сколько раз одни рискуют
    // больше других
    private const double DefenderRisk = 1.0;
    private const double MilitiaRisk = 0.4;
    private const double CivilianRisk = 0.15;
    private const double ChildRisk = 0.05;

    private const int VassalizationThresholdYears = 3; // Осада, длящаяся столько лет, может закончиться вассалитетом слабой стороны
    private const double VassalizationPowerRatio = 2.0; // Во сколько раз сильная сторона должна превосходить слабую по населению
    private const double VassalizationChance = 0.15; // Шанс в год, что явно проигрышная позиция обернётся вассалитетом
    private const double IndependenceChance = 0.25; // Шанс в год, что переросший вассал сбросит зависимость

    // Нрав государя решает не только налоги (см. TributeSystem.ChooseRate) — тот же
    // Trait.Brave/Prudent сказывается и на войне: смелый охотнее берётся за оружие,
    // осторожный ищет повод не браться. Множители на каждого претендента разом —
    // спор двух смелых государей эскалирует быстрее, чем спор смелого с осторожным
    private const double BraveRulerWarBoost = 1.3;
    private const double PrudentRulerWarCut = 0.7;

    // Ультиматум: явный перевес сил (см. TryGetLopsidedPair) даёт сильной стороне
    // повод потребовать покорности прямо у свежего спора, не дожидаясь года
    // осады, — та же вассализация (см. DeclareVassalization), только бескровная.
    // Слабая сторона решает по своему нраву (см. Trait) — то же Brave/Prudent,
    // что уже решает, ввязываться ли в войну вообще
    private const double UltimatumChance = 0.2; // Шанс, что сильный сосед выставит ультиматум свежему спору
    private const double BaseSubmitChance = 0.5;
    private const double PrudentSubmitBoost = 1.5;
    private const double BraveSubmitCut = 0.5;

    public static void Process(World world)
    {
        TryBreakFree(world);

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

            // Ультиматум — только свежему спору, ещё не ставшему осадой: если стороны
            // уже воюют годами, требовать покорности поздно, дело решает сама война
            if (settlement.SiegeYears == 0 && claimants.Count == 2 &&
                TryGetLopsidedPair(claimants[0], claimants[1], out var demandant, out var target) &&
                Rng.NextDouble() < UltimatumChance)
            {
                if (Rng.NextDouble() < GetSubmissionChance(target.Ruler))
                {
                    DeclareVassalization(settlement, target, demandant, world, viaUltimatum: true);
                    continue;
                }
                // Ультиматум отвергнут — год решает обычным порядком, как если бы его не было
            }

            var isHolyWar = IsReligiousDispute(claimants);
            var effectiveWarChance = isHolyWar ? world.Settings.WarChance * HolyWarChanceMultiplier : world.Settings.WarChance;

            // Спор родни решается тяжелее: воевать с домом, куда выдана сестра,
            // не то же самое, что с чужим (см. DynasticSystem)
            effectiveWarChance *= DynasticSystem.GetWarRestraint(claimants, world);
            effectiveWarChance *= GetRulerTemperamentFactor(claimants);

            // Осада ещё не началась — как и раньше, решает WarChance. Уже идущая
            // осада не бросает эту монету заново — раз начавшись, не может
            // случайно "передумать" и продолжается, пока не разрешится исходом
            if (settlement.SiegeYears == 0 && Rng.NextDouble() >= effectiveWarChance)
            {
                continue;
            }

            if (claimants.Count == 2 && settlement.SiegeYears >= VassalizationThresholdYears &&
                TryGetLopsidedPair(claimants[0], claimants[1], out var stronger, out var weaker) &&
                Rng.NextDouble() < VassalizationChance)
            {
                DeclareVassalization(settlement, weaker, stronger, world);
                continue;
            }

            if (settlement.SiegeYears >= TruceThresholdYears && Rng.NextDouble() < TruceChance)
            {
                DeclareTruce(settlement, claimants, world);
                continue;
            }

            settlement.SiegeYears++;

            DeclareWar(settlement, claimants, world, isHolyWar);
        }
    }

    // Вассалитет держится ровно на том, чем был куплен, — на перевесе сил.
    // Раньше связь снималась только с полным угасанием дома сюзерена, и
    // ослабевший господин держал вассала вечно, даже когда тот его перерос
    private static void TryBreakFree(World world)
    {
        foreach (var vassal in world.Kingdoms)
        {
            var suzerain = vassal.Suzerain;

            if (vassal.FallenYear != null || suzerain == null)
            {
                continue;
            }

            // Сюзерен пал — вассалитету не на чем держаться (страховка на случай,
            // если государство пало не через KingdomSystem, который тоже освобождает вассалов)
            if (suzerain.FallenYear != null)
            {
                vassal.Suzerain = null;
                continue;
            }

            // Зависимость держится ровно до тех пор, пока держится перевес, за
            // который она была куплена. Требовать от вассала обратного перевеса
            // было бы переворотом вчетверо — недостижимо для того, кто вошёл в
            // вассалитет именно потому, что был вдвое слабее
            if (GetPower(suzerain) >= GetPower(vassal) * VassalizationPowerRatio)
            {
                continue;
            }

            if (Rng.NextDouble() >= IndependenceChance)
            {
                continue;
            }

            vassal.Suzerain = null;

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Independence,
                Description = $"{vassal.Name} сбросило вассальную зависимость: {suzerain.Name} утратило прежний перевес",
                Kingdoms = [vassal, suzerain]
            });
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

    // Принять ультиматум или отвергнуть — решает нрав того, кому его выставили,
    // тем же приёмом, что и решение начать войну (см. GetRulerTemperamentFactor)
    private static double GetSubmissionChance(Character ruler)
    {
        var chance = BaseSubmitChance;

        if (ruler.Traits.Contains(Trait.Prudent))
        {
            chance *= PrudentSubmitBoost;
        }
        else if (ruler.Traits.Contains(Trait.Brave))
        {
            chance *= BraveSubmitCut;
        }

        return Math.Clamp(chance, 0, 1);
    }

    // Сила государства — живое население всех подконтрольных поселений
    private static int GetPower(Kingdom kingdom)
    {
        return kingdom.Settlements.SelectMany(s => s.Members).Count(m => m.Alive);
    }

    // Явно проигрышная позиция решается политически быстрее, чем истощает
    // обе стороны поровну (см. DeclareTruce) — слабая сторона признаёт
    // вассалитет сильной вместо продолжения бессмысленного сопротивления
    private static void DeclareVassalization(Settlement settlement, Kingdom weaker, Kingdom stronger, World world, bool viaUltimatum = false)
    {
        weaker.Suzerain = stronger;
        settlement.SiegeYears = 0;

        var reason = viaUltimatum ? "приняло ультиматум, не дожидаясь войны" : "признало вассалитет вместо продолжения войны";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Vassalization,
            Description = $"{settlement.Name}: {weaker.Name} {reason} — {stronger.Name} сильнее",
            Kingdoms = [weaker, stronger]
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
            Description = $"{settlement.Name}: затянувшаяся осада закончилась перемирием. Стороны: {claimantNames}",
            Kingdoms = claimants
        });
    }

    private static double GetRulerTemperamentFactor(List<Kingdom> claimants)
    {
        var factor = 1.0;

        foreach (var claimant in claimants)
        {
            if (claimant.Ruler.Traits.Contains(Trait.Brave))
            {
                factor *= BraveRulerWarBoost;
            }
            else if (claimant.Ruler.Traits.Contains(Trait.Prudent))
            {
                factor *= PrudentRulerWarCut;
            }
        }

        return factor;
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

    // Кого именно заберёт эта война. Выкашивает не всех подряд: гибнут прежде
    // всего те, кто вышел её встречать. Отбор взвешенный, а не по очереди —
    // гражданских задевает тоже, просто много реже, и когда защитники
    // кончаются, удар неминуемо приходится по остальным
    public static List<Character> PickCasualties(List<Character> residents, int count)
    {
        return residents
            .OrderByDescending(m => Math.Pow(Rng.NextDouble(), 1 / GetWarRisk(m)))
            .Take(count)
            .ToList();
    }

    // Во сколько раз этому жителю опаснее прочих оказаться среди погибших
    public static double GetWarRisk(Character character)
    {
        if (ProfessionSystem.GetCategory(character.Profession) == ProfessionCategory.Military)
        {
            return DefenderRisk;
        }

        if (character.LifeStage is LifeStage.Infant or LifeStage.Child or LifeStage.Student)
        {
            return ChildRisk;
        }

        // Ополчение собирают из тех, кого при осаде и правда ставят на стены
        if (character.LifeStage == LifeStage.Adult && character.Gender == Gender.Male)
        {
            return MilitiaRisk;
        }

        return CivilianRisk;
    }

    private static bool AreAtPeace(Kingdom a, Kingdom b)
    {
        return a.AlliedKingdoms.Contains(b) || a.Suzerain == b || b.Suzerain == a;
    }

    private static void DeclareWar(Settlement settlement, List<Kingdom> claimants, World world, bool isHolyWar)
    {
        var residents = settlement.Members.Where(m => m.Alive).ToList();

        var escalation = 1 + EscalationPerYear * Math.Min(settlement.SiegeYears, MaxEscalationYears);

        var defenders = residents.Where(m => ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military).ToList();

        // Обороняет не число голов, а умение гарнизона (см. ArmySystem):
        // десяток ветеранов на стенах стоит полусотни новобранцев
        var defenseFactor = 1 - Math.Min(MaxDefenseBonus, ArmySystem.GetGarrisonStrength(settlement, world) * DefenseBonusPerDefender);

        // Горы и холмы обороняют сами по себе, безо всякого гарнизона (см. TerrainSystem)
        var effectiveCasualtyRate = world.Settings.WarCasualtyRate * escalation * defenseFactor
            * WallSystem.GetWallFactor(settlement, world) * TerrainSystem.GetDefenseFactor(settlement, world);

        if (isHolyWar)
        {
            effectiveCasualtyRate *= HolyWarCasualtyMultiplier;
        }

        var casualtyCount = (int)(residents.Count * effectiveCasualtyRate);

        var casualties = PickCasualties(residents, casualtyCount);

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.War);
        }

        // Война не только убивает: уцелевших уводят (см. CaptiveSystem).
        // Уводит сильнейший из претендентов — тот, кому осада по средствам
        var captor = claimants.OrderByDescending(GetPower).ThenBy(k => k.Id).First();
        var survivors = residents.Where(m => m.Alive && !casualties.Contains(m)).ToList();

        CaptiveSystem.Process(settlement, survivors, casualtyCount, captor, world);

        // Стены принимают удар на себя и не выдерживают его бесследно — осада
        // постепенно срывает ту самую защиту, что делает её дорогой для нападающих
        if (settlement.Walls > 0 && Rng.NextDouble() < SiegeWallDamageChance)
        {
            settlement.Walls--;
        }

        // Боевое братство: выжившие защитники, отбившие осаду плечом к плечу, сближаются
        var survivingDefenders = defenders.Where(d => !casualties.Contains(d)).ToList();

        for (var i = 0; i < survivingDefenders.Count; i++)
        {
            for (var j = i + 1; j < survivingDefenders.Count; j++)
            {
                survivingDefenders[i].Friends.Add(survivingDefenders[j]);
                survivingDefenders[j].Friends.Add(survivingDefenders[i]);
            }
        }

        // Именительный падеж безопасен для любого числа претендентов —
        // "между X и Y" потребовало бы творительного, которого мы не умеем
        var claimantNames = string.Join(", ", claimants.Select(k => k.Name));
        var holyWarNote = isHolyWar ? " Война на почве веры." : "";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.War,
            Description = $"{settlement.Name}: {settlement.SiegeYears}-й год осады. Претенденты: {claimantNames}. Погибших: {casualties.Count}.{holyWarNote}",
            Kingdoms = claimants
        });
    }
}
