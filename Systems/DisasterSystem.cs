using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Катастрофы — редкие резкие потрясения поселения, в отличие от плавной
// вероятности обычной смертности/голода. Неурожай не убивает напрямую — только
// выбивает запас еды, а голодную цепочку последствий доводит до конца уже
// существующий EconomySystem (см. её описание), новой логики голода тут нет.
//
// Эпидемия, в отличие от неурожая, не остаётся в границах одного поселения:
// болезнь идёт туда, куда идут люди и товар. Каналов два, и оба уже есть в
// данных — торговые пути (TradeRoute) и просто соседство по карте (X/Y).
// Одних торговых путей мало: они складываются редко и поздно (нужны государство
// или союз и встречный дефицит с излишком), так что в большинстве вспышек
// партнёров у поселения попросту нет. Соседство работает всегда: между
// поселениями в дне пути люди ходят и без оформившейся торговли.
//
// Землетрясение и паводок — третий и четвёртый вид катастрофы, привязанные
// к рельефу (см. TerrainSystem), а не выпадающие всем поровну: горы трясёт,
// низину и приморье topит. Это оборотная сторона уже начисленной выгоды
// рельефа — горы дешевле не отдать силой (GetDefenseFactor), но однажды в
// сотни лет теряют дома и стены сами; пойма и приморье родят щедрее и
// вывозят больше (GetFertilityModifier, GetTradeCapacityMultiplier), но
// платят паводком, смывающим часть запасов. Ни один вид земли не остаётся
// только выигрышным
public static class DisasterSystem
{
    private const double ContagionChance = 0.35; // Шанс, что вспышка перекинется к связанному поселению
    private const double ContagionMortalityFactor = 0.6; // До соседа болезнь доходит ослабленной
    private const double ContagionDistance = 100; // Соседство, в пределах которого люди ходят и без торгового пути

    private const double EarthquakeCasualtyRate = 0.03; // Доля жителей, гибнущих при обрушении
    private const double EarthquakeBuildingLossShare = 0.3; // Доля домов и стен, рушащихся за одно землетрясение

    private const double FloodMaterialLossShare = 0.4; // Доля запасов сырья, смытых паводком

    public static void Process(World world)
    {
        // Свежие вспышки этого года не должны тут же расходиться дальше по цепочке:
        // болезнь распространяется по путям на один шаг за год, а не мгновенно
        // прокатывается через весь торговый союз
        var infectedThisYear = new HashSet<Settlement>();

        foreach (var settlement in world.Settlements)
        {
            var residents = settlement.Members.Where(m => m.Alive).ToList();

            if (residents.Count == 0)
            {
                continue;
            }

            if (Rng.NextDouble() >= world.Settings.DisasterChance)
            {
                continue;
            }

            TriggerRandomDisaster(settlement, residents, world, infectedThisYear);
        }

        foreach (var source in infectedThisYear)
        {
            Spread(source, world);
        }
    }

    // Эпидемия и неурожай грозят любой земле; землетрясение — только горам,
    // паводок — только низине и приморью (см. TerrainSystem.Relief). Выбор —
    // равная доля среди того, что вообще может случиться в этом месте
    private static void TriggerRandomDisaster(Settlement settlement, List<Character> residents, World world, HashSet<Settlement> infectedThisYear)
    {
        var relief = TerrainSystem.GetRelief(settlement, world);
        var roll = relief switch
        {
            Relief.Mountain => Rng.Next(3),
            Relief.Lowland or Relief.Coast => Rng.Next(3),
            _ => Rng.Next(2)
        };

        if (roll == 0)
        {
            TriggerEpidemic(settlement, residents, world);
            infectedThisYear.Add(settlement);
            return;
        }

        if (roll == 1)
        {
            TriggerCropFailure(settlement, residents, world);
            return;
        }

        if (relief == Relief.Mountain)
        {
            TriggerEarthquake(settlement, residents, world);
        }
        else
        {
            TriggerFlood(settlement, world);
        }
    }

    // Болезнь уходит к тем, с кем источник связан: к торговым партнёрам и к
    // ближайшим соседям по карте. Ослабленная — до соседа доезжают не все больные
    private static void Spread(Settlement source, World world)
    {
        var tradePartners = world.TradeRoutes
            .Where(r => r.A == source || r.B == source)
            .Select(r => r.A == source ? r.B : r.A);

        var neighbours = world.Settlements
            .Where(s => s != source && Distance(source, s) <= ContagionDistance);

        foreach (var partner in tradePartners.Union(neighbours))
        {
            var residents = partner.Members.Where(m => m.Alive).ToList();

            if (residents.Count == 0 || Rng.NextDouble() >= ContagionChance)
            {
                continue;
            }

            TriggerEpidemic(partner, residents, world, ContagionMortalityFactor, source);
        }
    }

    private static void TriggerEpidemic(
        Settlement settlement,
        List<Character> residents,
        World world,
        double severityFactor = 1.0,
        Settlement? broughtFrom = null)
    {
        var effectiveMortalityRate = world.Settings.EpidemicMortalityRate * HospitalSystem.GetHospitalFactor(settlement, world) * severityFactor;
        var casualtyCount = (int)(residents.Count * effectiveMortalityRate);

        var casualties = residents
            .OrderBy(_ => Rng.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.Disease);
        }

        // Стрелка вместо предлога — названия поселений не склоняем (см. MigrationSystem)
        var origin = broughtFrom == null ? "" : $" (занесена: {broughtFrom.Name} → {settlement.Name})";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: эпидемия, погибших — {casualties.Count}{origin}"
        });
    }

    private static double Distance(Settlement a, Settlement b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    private static void TriggerCropFailure(Settlement settlement, List<Character> residents, World world)
    {
        var loss = residents.Count * world.Settings.FoodConsumptionPerCapita * world.Settings.CropFailureFoodLossFactor;

        settlement.FoodStock -= loss;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: неурожай, потеряно {loss:F0} запаса еды"
        });
    }

    // Только горам (см. TerrainSystem.Relief.Mountain) — рушит то, что горы же
    // и накопили: дома и стены, а не запасы, которых в каменистой земле и так меньше
    private static void TriggerEarthquake(Settlement settlement, List<Character> residents, World world)
    {
        var casualtyCount = (int)(residents.Count * EarthquakeCasualtyRate);

        var casualties = residents
            .OrderBy(_ => Rng.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.Accident);
        }

        var housesLost = (int)Math.Ceiling(settlement.Houses * EarthquakeBuildingLossShare);
        var wallsLost = (int)Math.Ceiling(settlement.Walls * EarthquakeBuildingLossShare);

        settlement.Houses -= housesLost;
        settlement.Walls -= wallsLost;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: землетрясение, погибших — {casualties.Count}, " +
                          $"разрушено домов — {housesLost}, стен — {wallsLost}"
        });
    }

    // Только низине и приморью (см. TerrainSystem.Relief.Lowland/Coast) — смывает
    // запасы сырья, а не еду: недород урожая — уже неурожай, паводок бьёт по амбарам с товаром
    private static void TriggerFlood(Settlement settlement, World world)
    {
        double lost = 0;

        foreach (var type in settlement.MaterialStocks.Keys.ToList())
        {
            var loss = settlement.MaterialStocks[type] * FloodMaterialLossShare;

            settlement.MaterialStocks[type] -= loss;
            lost += loss;
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: паводок, смыто {lost:F0} запасов сырья"
        });
    }
}
