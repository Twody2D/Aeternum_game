using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Восстания: до сих пор действовать могли только элиты — короли воевали
// (WarSystem), родня плела заговоры (MurderSystem), претенденты резались за
// трон (KingdomSystem), а население всё это молча терпело. Здесь голос
// получают сами жители: поселение, которому невыносимо, отказывает короне
// в повиновении.
//
// Ни одного нового источника недовольства не заводим — все поводы уже есть в
// данных и уже используются другими системами: голод (FoodStock < 0, см.
// EconomySystem), чужая вера и чужая культура правителя (те же сравнения, что
// в KingdomSystem считает расколом государства) и затяжная осада (SiegeYears).
//
// Восставшее поселение не создаёт своего государства — оно просто перестаёт
// подчиняться: пока восстание длится, дань с него не идёт (TributeSystem), а
// само оно выпадает из подконтрольных территорий короны
public static class RebellionSystem
{
    private static readonly Random _random = new();

    private const double HungerWeight = 0.10; // Голодное поселение — первый и самый частый повод
    private const double AlienFaithWeight = 0.06; // Чужая вера правителя
    private const double AlienCultureWeight = 0.06; // Чужие обычаи правителя
    private const double SiegeWeight = 0.03; // ...за каждый год осады

    private const int MaxSiegeYearsCounted = 5; // Дальше терпение уже не ухудшается

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            foreach (var settlement in kingdom.Settlements.ToList())
            {
                if (settlement.RebellingUntilYear > world.CurrentYear)
                {
                    continue; // Уже восстало — второй раз подниматься не нужно
                }

                if (settlement.Members.Count(m => m.Alive) == 0)
                {
                    continue; // Восставать некому
                }

                var chance = GetDiscontent(settlement, kingdom);

                if (chance <= 0 || _random.NextDouble() >= chance)
                {
                    continue;
                }

                Rebel(settlement, kingdom, world);
            }
        }
    }

    // Сумма уже существующих в мире поводов быть недовольным короной
    private static double GetDiscontent(Settlement settlement, Kingdom kingdom)
    {
        var discontent = 0.0;

        if (settlement.FoodStock < 0)
        {
            discontent += HungerWeight;
        }

        var rulerSettlement = kingdom.Ruler.Settlement;

        if (settlement.Religion != null && rulerSettlement?.Religion != null && settlement.Religion != rulerSettlement.Religion)
        {
            discontent += AlienFaithWeight;
        }

        if (settlement.Culture != null && rulerSettlement?.Culture != null && settlement.Culture != rulerSettlement.Culture)
        {
            discontent += AlienCultureWeight;
        }

        discontent += Math.Min(settlement.SiegeYears, MaxSiegeYearsCounted) * SiegeWeight;

        return discontent;
    }

    private static void Rebel(Settlement settlement, Kingdom kingdom, World world)
    {
        settlement.RebellingUntilYear = world.CurrentYear + world.Settings.RebellionDuration;

        // Label-формат ("{Королевство}: ...") вместо склонения названия государства —
        // та же договорённость, что у DisasterSystem/WarSystem
        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Rebellion,
            Description = $"{kingdom.Name}: {settlement.Name} отказало короне в повиновении"
        });
    }

    // Поселение в открытом неповиновении короне — не платит дань и не считается
    // подконтрольной территорией (см. TributeSystem, KingdomSystem)
    public static bool IsRebelling(Settlement settlement, World world)
    {
        return settlement.RebellingUntilYear > world.CurrentYear;
    }
}
