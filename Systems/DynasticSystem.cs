using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Династический брак. Свадьбы в мире были, дома были, а политикой брак не был
// вовсе: женитьба наследника на дочери соседнего государя значила ровно
// столько же, сколько свадьба двух пахарей.
//
// Ничего нового для этого заводить не пришлось — родство домов выводится
// из уже существующих семей: два дома в родстве, если между ними есть живой
// брак, и притом брак знатных (см. EstateSystem). Оговорка про знать не
// украшение: дома в этом мире разрастаются на сотни человек, и по любому
// браку между их членами родственными оказались бы почти все со всеми —
// замер показал 33 союза из 39 "при родстве домов", то есть признак не
// различал ничего. Поэтому связь дают те браки, которые и правда заключаются
// как политические: между людьми при короне.
//
// Связь возникает и распадается сама: со смертью супругов родство домов
// истекает, если его не поддержали новые браки, — и союз, на нём стоявший,
// теряет опору.
//
// Последствий два, и оба в политике: породнившиеся дома охотнее заключают
// союз (см. AllianceSystem) и реже воюют между собой (см. WarSystem).
// Плюс сами такие браки становятся вероятнее: наследника выгоднее женить
// на чужом доме, чем на своей же соседке (см. MarriageSystem.GetAffinity)
public static class DynasticSystem
{
    // Во сколько раз охотнее сходятся дети правящих домов разных государств
    private const double DynasticMatchAffinity = 0.6;

    // Насколько родство домов помогает договориться и мешает воевать
    private const double KinAllianceBonus = 2.0;
    private const double KinWarRestraint = 0.4;

    // В родстве ли два дома: есть ли между ними живой брак
    public static bool AreHousesWed(Dynasty? a, Dynasty? b, World world)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }

        return world.Families.Any(f =>
            f.Father.Alive && f.Mother.Alive &&
            f.Father.CurrentFamily == f && f.Mother.CurrentFamily == f &&
            ((f.Father.Dynasty == a && f.Mother.Dynasty == b) ||
             (f.Father.Dynasty == b && f.Mother.Dynasty == a)) &&
            EstateSystem.GetEstate(f.Father, world) == Estate.Nobility &&
            EstateSystem.GetEstate(f.Mother, world) == Estate.Nobility);
    }

    public static bool AreRealmsWed(Kingdom a, Kingdom b, World world)
    {
        return AreHousesWed(a.Dynasty, b.Dynasty, world);
    }

    // Множитель к шансу союза между двумя государствами
    public static double GetAllianceFactor(Kingdom a, Kingdom b, World world)
    {
        return AreRealmsWed(a, b, world) ? KinAllianceBonus : 1.0;
    }

    // Множитель к шансу, что спор породнившихся домов дойдёт до войны
    public static double GetWarRestraint(List<Kingdom> claimants, World world)
    {
        for (var i = 0; i < claimants.Count; i++)
        {
            for (var j = i + 1; j < claimants.Count; j++)
            {
                if (AreRealmsWed(claimants[i], claimants[j], world))
                {
                    return KinWarRestraint;
                }
            }
        }

        return 1.0;
    }

    // Надбавка к взаимной склонности для пары, способной связать два престола.
    // Считается только для правящих домов разных государств: свадьба внутри
    // одного дома политикой не является
    public static double GetMatchAffinity(Character a, Character b, World world)
    {
        var realmA = FindRealm(a, world);
        var realmB = FindRealm(b, world);

        return realmA != null && realmB != null && realmA != realmB ? DynasticMatchAffinity : 0;
    }

    private static Kingdom? FindRealm(Character character, World world)
    {
        return character.Dynasty == null
            ? null
            : world.Kingdoms.FirstOrDefault(k => k.FallenYear == null && k.Dynasty == character.Dynasty);
    }
}
