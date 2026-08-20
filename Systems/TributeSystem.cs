using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Дань и то, на что она идёт. Ставка больше не одна на весь мир: каждый
// правитель держит свою и пересматривает её каждый год по обстоятельствам —
// пустеющая казна и война толкают её вверх, полная казна позволяет ослабить
// хватку, а характер правителя (см. Trait) сдвигает решение в ту или другую
// сторону. Подданные это чувствуют: поборы сверх привычной меры идут прямо
// в счёт недовольства короной (см. RebellionSystem).
//
// Обратный поток тоже здесь: в голодный год корона кормит свои земли из
// казны. Без этого запасать было незачем — казна росла мёртвым грузом и
// значила лишь устойчивость трона (см. KingdomSystem.TryTriggerSuccessionCrisis).
// Теперь у высоких налогов есть смысл, а у скупости — цена
public static class TributeSystem
{
    private const double VassalTributeRate = 0.2; // Доля уже собранной в этом году казны, которую вассал платит сюзерену сверху обычной дани

    private const double MinRate = 0.02; // Совсем без дани не обходится никто...
    private const double MaxRate = 0.4; // ...но и обобрать землю дочиста не даст даже самый жадный

    private const double LowTreasuryShare = 0.25; // Казна ниже этой доли своей вместимости считается пустеющей
    private const double HighTreasuryShare = 0.75; // ...а выше этой — полной (см. StorageSystem)

    private const double EmptyTreasuryRaise = 0.08; // Насколько правитель поднимает ставку, когда казна пустеет
    private const double FullTreasuryCut = 0.05; // ...и насколько ослабляет, когда она полна
    private const double WartimeRaise = 0.08; // Осада или мятеж в землях — повод затянуть пояса подданным

    private const double BoldRulerShift = 0.05; // Смелый берёт больше, не опасаясь недовольства
    private const double PrudentRulerShift = 0.05; // Осторожный предпочитает не злить землю

    private const double ReliefShare = 0.5; // Какую часть казны корона готова раздать за один голодный год

    // До какого запаса корона подтягивает нуждающиеся земли: год их собственного
    // прокорма. Не только спасение от голода — иначе помощь дублировала бы обмен
    // излишками между провинциями (см. TradeSystem), который закрывает дефицит раньше
    private const double ReliefTargetYears = 1.0;

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            kingdom.TributeRate = ChooseRate(kingdom, world);

            // Казначей знает, где что лежит: та же ставка приносит короне больше
            // (см. CourtSystem). Подданным от этого не легче и не тяжелее —
            // недовольство считается по ставке, а не по собранному
            var collection = CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Treasurer, world);

            foreach (var settlement in kingdom.Settlements)
            {
                if (RebellionSystem.IsRebelling(settlement, world))
                {
                    continue; // Восставшие короне не платят
                }

                // ...а до дальней окраины рука короны дотягивается хуже (см. CapitalSystem)
                var reach = collection * CapitalSystem.GetControl(kingdom, settlement);

                if (settlement.FoodStock > 0)
                {
                    var foodTribute = settlement.FoodStock * kingdom.TributeRate * reach;
                    settlement.FoodStock -= foodTribute;
                    kingdom.FoodTreasury += foodTribute;
                }

                if (settlement.Gold > 0)
                {
                    var goldTribute = settlement.Gold * kingdom.TributeRate * reach;
                    settlement.Gold -= goldTribute;
                    kingdom.GoldTreasury += goldTribute;
                }

                foreach (var type in Enum.GetValues<MaterialType>())
                {
                    var stock = settlement.MaterialStocks.GetValueOrDefault(type);

                    if (stock <= 0)
                    {
                        continue;
                    }

                    var materialTribute = stock * kingdom.TributeRate * reach;
                    settlement.MaterialStocks[type] = stock - materialTribute;
                    kingdom.MaterialTreasury[type] = kingdom.MaterialTreasury.GetValueOrDefault(type) + materialTribute;
                }
            }
        }

        // Вассальная дань — поверх обычной, из уже собранной в этом году казны
        // вассала (казна → казна вместо поселение → казна)
        foreach (var vassal in world.Kingdoms)
        {
            if (vassal.FallenYear != null || vassal.Suzerain == null || vassal.Suzerain.FallenYear != null)
            {
                continue;
            }

            var suzerain = vassal.Suzerain;

            var foodTribute = vassal.FoodTreasury * VassalTributeRate;
            vassal.FoodTreasury -= foodTribute;
            suzerain.FoodTreasury += foodTribute;

            var goldTribute = vassal.GoldTreasury * VassalTributeRate;
            vassal.GoldTreasury -= goldTribute;
            suzerain.GoldTreasury += goldTribute;

            foreach (var type in Enum.GetValues<MaterialType>())
            {
                var stock = vassal.MaterialTreasury.GetValueOrDefault(type);

                if (stock <= 0)
                {
                    continue;
                }

                var materialTribute = stock * VassalTributeRate;
                vassal.MaterialTreasury[type] = stock - materialTribute;
                suzerain.MaterialTreasury[type] = suzerain.MaterialTreasury.GetValueOrDefault(type) + materialTribute;
            }
        }

        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear == null)
            {
                RelieveFamine(kingdom, world);
            }
        }
    }

    // Ставка, которую правитель держит в этом году. Все доводы взяты из уже
    // существующего состояния: насколько полна казна относительно того, сколько
    // корона вообще способна хранить (см. StorageSystem), воюют ли её земли,
    // и каков нрав самого правителя
    public static double ChooseRate(Kingdom kingdom, World world)
    {
        var rate = world.Settings.TributeRate;

        // Богатство короны — то же, чем меряется устойчивость трона
        // (см. KingdomSystem): зерно в амбарах и золото в сундуках. Меряем
        // его тем, сколько корона вообще способна хранить (см. StorageSystem)
        var capacity = StorageSystem.GetTreasuryCapacity(kingdom);
        var fill = capacity > 0 ? (kingdom.FoodTreasury + kingdom.GoldTreasury) / capacity : 0;

        if (fill < LowTreasuryShare)
        {
            rate += EmptyTreasuryRaise;
        }
        else if (fill > HighTreasuryShare)
        {
            rate -= FullTreasuryCut;
        }

        if (IsInTrouble(kingdom, world))
        {
            rate += WartimeRaise;
        }

        if (kingdom.Ruler.Traits.Contains(Trait.Brave))
        {
            rate += BoldRulerShift;
        }

        if (kingdom.Ruler.Traits.Contains(Trait.Prudent))
        {
            rate -= PrudentRulerShift;
        }

        return Math.Clamp(rate, MinRate, MaxRate);
    }

    // Сколько зерна не хватает поселению до годового прокорма его жителей
    private static double GetReliefNeed(Settlement settlement, World world)
    {
        var residents = settlement.Members.Count(m => m.Alive);

        if (residents == 0)
        {
            return 0; // Кормить некого
        }

        var target = residents * world.Settings.FoodConsumptionPerCapita * ReliefTargetYears;

        return Math.Max(0, target - settlement.FoodStock);
    }

    // Осада в своих землях либо открытый мятеж — то, что требует денег немедленно
    private static bool IsInTrouble(Kingdom kingdom, World world)
    {
        return kingdom.Settlements.Any(s => s.SiegeYears > 0)
               || world.Settlements.Any(s => s.RebellingAgainst == kingdom);
    }

    // Корона подтягивает свои беднейшие земли до годового прокорма. Раздаёт
    // не всё — оставшееся нужно ей самой на походы и на следующий год,
    // а восставшие земли короне уже не земли
    private static void RelieveFamine(Kingdom kingdom, World world)
    {
        if (kingdom.FoodTreasury <= 0)
        {
            return;
        }

        var needy = kingdom.Settlements
            .Where(s => !RebellionSystem.IsRebelling(s, world))
            .Select(s => (Settlement: s, Need: GetReliefNeed(s, world)))
            .Where(x => x.Need > 0)
            .OrderByDescending(x => x.Need) // Самым бедным — первым
            .ToList();

        if (needy.Count == 0)
        {
            return;
        }

        var available = kingdom.FoodTreasury * ReliefShare;
        var given = 0.0;

        foreach (var (settlement, need) in needy)
        {
            if (available <= 0)
            {
                break;
            }

            var share = Math.Min(need, available);

            settlement.FoodStock += share;
            kingdom.FoodTreasury -= share;
            available -= share;
            given += share;
        }

        if (given <= 0)
        {
            return;
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Relief,
            Description = $"{kingdom.Name}: корона раздала {given:0.#} хлеба нуждающимся землям ({needy.Count})",
            Kingdoms = [kingdom]
        });
    }
}
