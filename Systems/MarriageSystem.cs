using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;


// Заключение браков раз в год. Пары подбираются в первую очередь внутри одного
// поселения; те, кому не нашлось пары дома, реже, но могут найти её в другом
// поселении — невеста переезжает туда, где живёт муж (см. RelocateBride).
//
// Внутри доступного круга выбор не случаен: считается взаимная склонность,
// и человек берёт того, к кому её больше. Ничего нового для этого заводить
// не пришлось — всё уже было в мире: дружба (Character.Friends), общий круг
// знакомых, сходство нрава (Character.Traits) и разница в возрасте. Та же
// склонность решает, состоится ли брак вообще, и она же потом удерживает
// семью от распада (см. DivorceSystem)
public static class MarriageSystem
{
    private const int SameSettlementChancePercent = 50;
    private const int CrossSettlementChancePercent = 25; // Реже — переезд в другое поселение не так прост
    private const double DifferentReligionPenalty = 0.5; // Доп. множитель к межпоселенческому браку при разных религиях сторон
    private const double DifferentCulturePenalty = 0.5; // Доп. множитель при разных традициях сторон — независим от религии, штрафы перемножаются

    private const double FriendshipAffinity = 0.8; // Дружба — самый весомый довод: этих двоих уже свела жизнь
    private const double SharedFriendAffinity = 0.15; // Общий круг знакомых сближает
    private const double MaxSharedFriendAffinity = 0.45;
    private const double SharedTraitAffinity = 0.2; // Сходство нрава
    private const double AgeGapPenalty = 0.03; // ...за каждый год разницы в возрасте
    private const double MaxAgeGapPenalty = 0.6;

    private const double SameEstateAffinity = 0.3; // Равные тянутся к равным...
    private const double EstateGapPenalty = 0.35; // ...а через сословие переступают неохотно

    private const double MinAffinity = 0.1; // Совсем безнадёжных пар не бывает...
    private const double MaxAffinity = 2.5; // ...как и предрешённых

    private static readonly string[] DescriptionTemplates =
    {
        "{0} и {1} создали семью",
        "{0} и {1} сыграли свадьбу"
    };


    public static void Process(World world)
    {
        var availableMen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Male &&
                c.Age >= world.Settings.AdultAge &&
                c.Age <= 60 &&
                c.CurrentFamily == null)
            .ToList();

        var availableWomen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Female &&
                c.Age >= world.Settings.AdultAge &&
                c.Age <= 45 &&
                c.CurrentFamily == null)
            .ToList();

        // Первый проход: пары внутри одного поселения
        foreach (var settlementMen in availableMen.GroupBy(c => c.Settlement))
        {
            var women = availableWomen
                .Where(w => w.Settlement == settlementMen.Key && w.CurrentFamily == null)
                .OrderBy(x => Rng.Next())
                .ToList();

            var men = settlementMen.OrderBy(x => Rng.Next()).ToList();

            MarryWithinGroup(men, women, world, SameSettlementChancePercent);
        }

        // Второй проход: кто не нашёл пару дома, пробует за пределами своего поселения
        var leftoverMen = availableMen.Where(c => c.CurrentFamily == null).OrderBy(x => Rng.Next()).ToList();
        var leftoverWomen = availableWomen.Where(c => c.CurrentFamily == null).OrderBy(x => Rng.Next()).ToList();

        MarryWithinGroup(leftoverMen, leftoverWomen, world, CrossSettlementChancePercent);
    }

    private static void MarryWithinGroup(List<Character> men, List<Character> women, World world, int marriageChancePercent)
    {
        var takenWomen = new HashSet<Character>();

        foreach (var man in men)
        {
            // Из доступных берётся не первая попавшаяся, а та, к кому больше склонности
            var woman = women
                .Where(w => !takenWomen.Contains(w) && !AreRelated(man, w))
                .OrderByDescending(w => GetAffinity(man, w, world))
                .ThenBy(w => w.Id)
                .FirstOrDefault();

            if (woman == null)
            {
                continue;
            }

            // Считаем пару сформированной вне зависимости от исхода броска,
            // чтобы один и тот же человек не участвовал в нескольких парах за год
            takenWomen.Add(woman);

            var effectiveChancePercent = marriageChancePercent * GetAffinity(man, woman, world);

            if (man.Settlement?.Religion != null &&
                woman.Settlement?.Religion != null &&
                man.Settlement.Religion != woman.Settlement.Religion)
            {
                effectiveChancePercent *= DifferentReligionPenalty;
            }

            if (man.Settlement?.Culture != null &&
                woman.Settlement?.Culture != null &&
                man.Settlement.Culture != woman.Settlement.Culture)
            {
                effectiveChancePercent *= DifferentCulturePenalty;
            }

            if (Rng.Next(100) >= effectiveChancePercent)
            {
                continue;
            }

            RelocateBride(woman, man.Settlement);

            FamilySystem.CreateFamily(
                woman,
                man,
                world
            );

            var template = DescriptionTemplates[Rng.Next(DescriptionTemplates.Length)];

            world.Events.Add(
                new WorldEvent
                {
                    Year = world.CurrentYear,

                    Type = EventType.Marriage,

                    Description = string.Format(
                        template,
                        SurnameSystem.GetDisplayFullName(man),
                        SurnameSystem.GetDisplayFullName(woman))
                }
            );
        }
    }

    // Взаимная склонность двоих: во сколько раз охотнее они пойдут под венец
    // друг с другом, чем со случайным встречным. Единица — полное безразличие
    public static double GetAffinity(Character a, Character b, World world)
    {
        var affinity = 1.0;

        // Сословие ничем не записано — оно вычисляется по нынешнему положению
        // обоих (см. EstateSystem), поэтому и мезальянс возникает сам собой
        var estateGap = Math.Abs(EstateSystem.GetEstate(a, world) - EstateSystem.GetEstate(b, world));

        affinity += estateGap == 0 ? SameEstateAffinity : -estateGap * EstateGapPenalty;

        // Брак, способный связать два престола, стоит дороже прочих (см. DynasticSystem)
        affinity += DynasticSystem.GetMatchAffinity(a, b, world);

        if (a.Friends.Contains(b))
        {
            affinity += FriendshipAffinity;
        }

        var sharedFriends = a.Friends.Count(f => f.Alive && b.Friends.Contains(f));

        affinity += Math.Min(MaxSharedFriendAffinity, sharedFriends * SharedFriendAffinity);

        var sharedTraits = a.Traits.Count(t => b.Traits.Contains(t));

        affinity += sharedTraits * SharedTraitAffinity;

        // Чем дальше друг от друга по возрасту, тем меньше общего
        affinity -= Math.Min(MaxAgeGapPenalty, Math.Abs(a.Age - b.Age) * AgeGapPenalty);

        return Math.Clamp(affinity, MinAffinity, MaxAffinity);
    }

    // Невеста переезжает в поселение мужа (если оно другое). Прежнее поселение
    // не забывает её — Settlement.Members хранит всех, кто там когда-либо жил
    private static void RelocateBride(Character bride, Settlement? husbandSettlement)
    {
        if (husbandSettlement == null || bride.Settlement == husbandSettlement)
        {
            return;
        }

        bride.Settlement = husbandSettlement;
        husbandSettlement.Members.Add(bride);
    }

    // Запрет браков между близкими родственниками (родитель/ребёнок, братья/сёстры)
    // и между враждующими семьями (см. Character.Enemies, MurderSystem.AddEnmity)
    private static bool AreRelated(Character a, Character b)
    {
        if (a.Enemies.Contains(b))
        {
            return true;
        }

        if (a.Mother == b || a.Father == b || b.Mother == a || b.Father == a)
        {
            return true;
        }

        if (a.Mother != null && (a.Mother == b.Mother || a.Mother == b.Father))
        {
            return true;
        }

        if (a.Father != null && (a.Father == b.Mother || a.Father == b.Father))
        {
            return true;
        }

        return false;
    }
}
