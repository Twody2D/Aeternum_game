using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Сословия. Все жители мира до сих пор были равны между собой: разница
// была только в занятии и в родстве, но ни во что общественное она
// не складывалась.
//
// Отдельного поля сословие не получило — оно целиком выводится из того,
// кем человек приходится миру сейчас: знать это служащие короне и их дети
// (см. CourtSystem), горожане — те, кто живёт умением рук или головы,
// остальные простолюдины. Поэтому сословие само меняется вместе
// с судьбой: угасший дом перестаёт быть знатью, а ремесленник, ушедший
// в поле от голода (см. CareerSystem), перестаёт быть горожанином.
//
// Последствия два, и оба там, где неравенство и должно быть заметно:
// в браке (равные тянутся к равным, см. MarriageSystem.GetAffinity) и в голод
// (до чужих запасов и связей нужда добирается последней, см. EconomySystem).
// Третьего — наследования занятия по сословию — заводить не понадобилось:
// сын книжника и без того чаще берётся за книгу, потому что ремесло родителя
// передаётся напрямую (см. ProfessionSystem.GetRandom)
public static class EstateSystem
{
    // Во сколько раз реже знать гибнет от голода: до её запасов нужда
    // добирается последней (см. EconomySystem)
    private const double NobilityStarvationShield = 0.3;
    private const double BurghersStarvationShield = 0.7;

    public static Estate GetEstate(Character character, World world)
    {
        // Знатность даёт служба короне и одно поколение после неё: сам при
        // короне либо сын или дочь того, кто при ней. Через правящий дом
        // считать нельзя — за века он разрастается на весь мир, и знатью
        // оказываются поголовно все (проверено замером: 121 из 121)
        if (ServesTheCrown(character, world) ||
            (character.Father != null && ServesTheCrown(character.Father, world)) ||
            (character.Mother != null && ServesTheCrown(character.Mother, world)))
        {
            return Estate.Nobility;
        }

        return ProfessionSystem.GetCategory(character.Profession) switch
        {
            ProfessionCategory.Craft or ProfessionCategory.Trade or ProfessionCategory.Knowledge => Estate.Burghers,
            _ => Estate.Commoners
        };
    }

    // Правитель или тот, кто занимает должность при его дворе (см. CourtSystem)
    private static bool ServesTheCrown(Character character, World world)
    {
        return world.Kingdoms.Any(k => k.FallenYear == null
                                       && (k.Ruler == character || k.Court.ContainsValue(character)));
    }

    // Множитель к риску умереть от голода: у кого есть запас и связи,
    // тот переживает недород легче
    public static double GetStarvationShield(Character character, World world)
    {
        return GetEstate(character, world) switch
        {
            Estate.Nobility => NobilityStarvationShield,
            Estate.Burghers => BurghersStarvationShield,
            _ => 1.0
        };
    }

    public static string GetName(Estate estate)
    {
        return estate switch
        {
            Estate.Nobility => "знать",
            Estate.Burghers => "горожане",
            _ => "простолюдины"
        };
    }
}
