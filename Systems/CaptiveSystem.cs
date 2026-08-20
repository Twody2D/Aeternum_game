using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Война умела только убивать. Между тем угон людей был её обычным исходом
// и стоил осаждающему дешевле резни: пленный работает, пленного меняют,
// за знатного платят выкуп.
//
// Кого уводят, решает та же шкала, что решает, кто погибнет
// (см. WarSystem.GetWarRisk), только наоборот: бойцов на стенах убивают,
// а уводят тех, кто не отбивается, — за ними и приходят. Уводит сильнейший
// из претендентов, к себе в столицу (см. CapitalSystem).
//
// Дальше пленный живёт обычной жизнью мира: он числится жителем чужого
// поселения, работает, женится, а вот наречие вокруг него — чужое, и
// со временем именно из таких переселений и растут смешанные земли
// (см. LanguageSystem).
//
// Выкуп берётся из казны того государства, что владело поселением
// (см. TributeSystem): скупая корона своих не выкупает
public static class CaptiveSystem
{
    private const double CaptiveRate = 0.5; // Уводят примерно столько же, сколько убивают
    private const double NobleRansom = 200; // Во что обходится казне возвращение одного своего знатного

    public static void Process(Settlement settlement, List<Character> survivors, int casualtyCount, Kingdom captor, World world)
    {
        var captiveCount = (int)(casualtyCount * CaptiveRate);

        if (captiveCount <= 0 || survivors.Count == 0)
        {
            return;
        }

        var destination = captor.Capital;

        if (destination == null || destination == settlement)
        {
            return; // Уводить некуда — своих земель у победителя не осталось
        }

        // Приходят за теми, кто не отбивается: шкала военного риска, вывернутая наизнанку
        var captives = survivors
            .OrderByDescending(m => Math.Pow(Rng.NextDouble(), WarSystem.GetWarRisk(m)))
            .Take(captiveCount)
            .ToList();

        var ransomed = 0;
        var taken = 0;

        foreach (var captive in captives)
        {
            if (TryRansom(captive, settlement, world))
            {
                ransomed++;
                continue;
            }

            Relocate(captive, destination);
            taken++;
        }

        if (taken == 0 && ransomed == 0)
        {
            return;
        }

        var ransomNote = ransomed > 0 ? $", выкуплено {ransomed}" : "";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Captivity,
            Description = $"{settlement.Name}: {captor.Name} уводит пленных — {taken} человек{ransomNote}"
        });
    }

    // Выкупают знатных, за прочих корона не платит. Сперва выкуп полагался
    // всякому пленному — и замер показал, что тогда уводить перестают вовсе:
    // казна к тому времени богата, и все двадцать из двадцати трёх угонов
    // кончались откупом. Сословие (см. EstateSystem) возвращает механике смысл:
    // знатного возвращают за золото, простолюдина уводят
    private static bool TryRansom(Character captive, Settlement settlement, World world)
    {
        if (EstateSystem.GetEstate(captive, world) != Estate.Nobility)
        {
            return false;
        }

        var crown = world.Kingdoms.FirstOrDefault(k => k.FallenYear == null && k.Settlements.Contains(settlement));

        if (crown == null || crown.GoldTreasury < NobleRansom)
        {
            return false;
        }

        crown.GoldTreasury -= NobleRansom;

        return true;
    }

    private static void Relocate(Character captive, Settlement destination)
    {
        captive.Settlement = destination;

        if (!destination.Members.Contains(captive))
        {
            destination.Members.Add(captive);
        }
    }

}
