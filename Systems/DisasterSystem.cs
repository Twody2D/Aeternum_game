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
// поселениями в дне пути люди ходят и без оформившейся торговли
public static class DisasterSystem
{
    private static readonly Random _random = new();

    private const double ContagionChance = 0.35; // Шанс, что вспышка перекинется к связанному поселению
    private const double ContagionMortalityFactor = 0.6; // До соседа болезнь доходит ослабленной
    private const double ContagionDistance = 100; // Соседство, в пределах которого люди ходят и без торгового пути

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

            if (_random.NextDouble() >= world.Settings.DisasterChance)
            {
                continue;
            }

            if (_random.Next(2) == 0)
            {
                TriggerEpidemic(settlement, residents, world);
                infectedThisYear.Add(settlement);
            }
            else
            {
                TriggerCropFailure(settlement, residents, world);
            }
        }

        foreach (var source in infectedThisYear)
        {
            Spread(source, world);
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

            if (residents.Count == 0 || _random.NextDouble() >= ContagionChance)
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
        var effectiveMortalityRate = world.Settings.EpidemicMortalityRate * HospitalSystem.GetHospitalFactor(settlement) * severityFactor;
        var casualtyCount = (int)(residents.Count * effectiveMortalityRate);

        var casualties = residents
            .OrderBy(_ => _random.Next())
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
}
