using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Союзы между государствами: без карты/географии единственный существующий
// сигнал для сближения — тот же, что уже используется в MarriageSystem для
// межпоселенческих браков, только в обратную сторону: общая религия правящих
// домов (Kingdom не хранит свою религию — берём религию поселения, где живёт
// действующий правитель). Союз здесь не разрывается — это может стать
// отдельным следующим шагом
public static class AllianceSystem
{
    private const double AllianceBreakChance = 0.1; // Шанс в год, что союз распадётся, пока правящие дома разошлись в вере

    public static void Process(World world)
    {
        var activeKingdoms = world.Kingdoms.Where(k => k.FallenYear == null).ToList();

        for (var i = 0; i < activeKingdoms.Count; i++)
        {
            for (var j = i + 1; j < activeKingdoms.Count; j++)
            {
                if (activeKingdoms[i].AlliedKingdoms.Contains(activeKingdoms[j]))
                {
                    TryBreakAlliance(activeKingdoms[i], activeKingdoms[j], world);
                }
                else
                {
                    TryFormAlliance(activeKingdoms[i], activeKingdoms[j], world);
                }
            }
        }
    }

    private static void TryFormAlliance(Kingdom a, Kingdom b, World world)
    {
        var religionA = GetRulerReligion(a);
        var religionB = GetRulerReligion(b);

        if (religionA == null || religionA != religionB)
        {
            return;
        }

        // Общая вера сводит, общий язык помогает договориться, родство домов
        // добавляет прямой интерес (см. DynasticSystem)
        var chance = world.Settings.AllianceChance
                     * LanguageSystem.GetDiplomacyFactor(a, b)
                     * DynasticSystem.GetAllianceFactor(a, b, world);

        if (Rng.NextDouble() >= chance)
        {
            return;
        }

        a.AlliedKingdoms.Add(b);
        b.AlliedKingdoms.Add(a);

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Alliance,
            Description = $"{a.Name} и {b.Name} заключили союз на почве общей веры" +
                          (LanguageSystem.SharesLanguage(a.Ruler.Settlement, b.Ruler.Settlement) ? " и общего наречия" : "") +
                          (DynasticSystem.AreRealmsWed(a, b, world) ? " (дома в родстве)" : "")
        });
    }

    // Союз держится на той же общей вере, что его создала (см. TryFormAlliance) —
    // если после смены правителя (KingdomSystem.UpdateExistingKingdoms) вера разошлась,
    // союз не рвётся мгновенно, но с каждым годом расхождения рискует распасться
    private static void TryBreakAlliance(Kingdom a, Kingdom b, World world)
    {
        var religionA = GetRulerReligion(a);
        var religionB = GetRulerReligion(b);

        if (religionA != null && religionA == religionB)
        {
            return; // Вера всё ещё общая — союз держится
        }

        if (Rng.NextDouble() >= AllianceBreakChance)
        {
            return;
        }

        a.AlliedKingdoms.Remove(b);
        b.AlliedKingdoms.Remove(a);

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.AllianceBroken,
            Description = $"{a.Name} и {b.Name} разорвали союз: правящие дома разошлись в вере"
        });
    }

    private static Religion? GetRulerReligion(Kingdom kingdom)
    {
        return kingdom.Ruler.Settlement?.Religion;
    }
}
