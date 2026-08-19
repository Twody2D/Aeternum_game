using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;


// Заключение браков раз в год. Пары подбираются в первую очередь внутри одного
// поселения; те, кому не нашлось пары дома, реже, но могут найти её в другом
// поселении — невеста переезжает туда, где живёт муж (см. RelocateBride)
public static class MarriageSystem
{
    private static readonly Random _random = new();

    private const int SameSettlementChancePercent = 50;
    private const int CrossSettlementChancePercent = 25; // Реже — переезд в другое поселение не так прост
    private const double DifferentReligionPenalty = 0.5; // Доп. множитель к межпоселенческому браку при разных религиях сторон

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
                .OrderBy(x => _random.Next())
                .ToList();

            var men = settlementMen.OrderBy(x => _random.Next()).ToList();

            MarryWithinGroup(men, women, world, SameSettlementChancePercent);
        }

        // Второй проход: кто не нашёл пару дома, пробует за пределами своего поселения
        var leftoverMen = availableMen.Where(c => c.CurrentFamily == null).OrderBy(x => _random.Next()).ToList();
        var leftoverWomen = availableWomen.Where(c => c.CurrentFamily == null).OrderBy(x => _random.Next()).ToList();

        MarryWithinGroup(leftoverMen, leftoverWomen, world, CrossSettlementChancePercent);
    }

    private static void MarryWithinGroup(List<Character> men, List<Character> women, World world, int marriageChancePercent)
    {
        var takenWomen = new HashSet<Character>();

        foreach (var man in men)
        {
            var woman = women.FirstOrDefault(w =>
                !takenWomen.Contains(w) &&
                !AreRelated(man, w));

            if (woman == null)
            {
                continue;
            }

            // Считаем пару сформированной вне зависимости от исхода броска,
            // чтобы один и тот же человек не участвовал в нескольких парах за год
            takenWomen.Add(woman);

            var effectiveChancePercent = marriageChancePercent;

            if (man.Settlement?.Religion != null &&
                woman.Settlement?.Religion != null &&
                man.Settlement.Religion != woman.Settlement.Religion)
            {
                effectiveChancePercent = (int)(marriageChancePercent * DifferentReligionPenalty);
            }

            if (_random.Next(100) >= effectiveChancePercent)
            {
                continue;
            }

            RelocateBride(woman, man.Settlement);

            FamilySystem.CreateFamily(
                woman,
                man,
                world
            );

            var template = DescriptionTemplates[_random.Next(DescriptionTemplates.Length)];

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
