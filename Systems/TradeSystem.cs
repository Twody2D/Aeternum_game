using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Торговля внутри общего рынка: без карты/географии единственная существующая
// группировка поселений — Kingdom.Settlements, расширенная на союзников
// (AllianceSystem) транзитивно — если А союзно Б, а Б союзно В, все трое торгуют
// на одном общем рынке, а не в трёх раздельных. Сглаживает дефицит/излишки еды
// и материалов — государства и их союзы теперь дают не только политическую,
// но и экономическую пользу. Ничего не логирует — тот же принцип, что и у
// EconomySystem (перераспределение не событие, событие — только заметное
// последствие вроде голода)
public static class TradeSystem
{
    private const double RouteBonusPerYear = 0.05; // Наезженный путь год от года снижает потери при обмене
    private const double MaxRouteBonus = 0.5; // Потолок — до +50% к эффективности передачи

    public static void Process(World world)
    {
        var visited = new HashSet<Kingdom>();

        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null || visited.Contains(kingdom))
            {
                continue;
            }

            var cluster = CollectTradeCluster(kingdom, visited);
            var settlements = cluster.SelectMany(k => k.Settlements).Distinct().ToList();

            if (settlements.Count < 2)
            {
                continue;
            }

            // Redistribute вызывается 7 раз за год (еда + каждый MaterialType) — собираем
            // все пары, которые в этом году хоть раз реально поторговали, и крепим путь
            // ровно один раз за год на пару, а не один раз за каждый вид товара
            var tradedPairs = new HashSet<(Settlement, Settlement)>();

            tradedPairs.UnionWith(Redistribute(settlements, s => s.FoodStock, (s, v) => s.FoodStock = v, world.Settings.TradeTransferRate, world));

            foreach (var type in Enum.GetValues<MaterialType>())
            {
                tradedPairs.UnionWith(Redistribute(
                    settlements,
                    s => s.MaterialStocks.GetValueOrDefault(type),
                    (s, v) => s.MaterialStocks[type] = v,
                    world.Settings.TradeTransferRate,
                    world));
            }

            foreach (var (a, b) in tradedPairs)
            {
                GetOrCreateRoute(a, b, world).Years++;
            }
        }
    }

    // Путь — ненаправленная пара (A/B симметричны, как Character.Enemies), создаётся при первой торговле
    private static TradeRoute GetOrCreateRoute(Settlement a, Settlement b, World world)
    {
        var route = world.TradeRoutes.FirstOrDefault(r => (r.A == a && r.B == b) || (r.A == b && r.B == a));

        if (route != null)
        {
            return route;
        }

        route = new TradeRoute { A = a, B = b, Years = 0 };
        world.TradeRoutes.Add(route);

        return route;
    }

    private static double GetRouteFactor(Settlement a, Settlement b, World world)
    {
        var route = world.TradeRoutes.FirstOrDefault(r => (r.A == a && r.B == b) || (r.A == b && r.B == a));

        return route == null ? 1 : 1 + Math.Min(MaxRouteBonus, route.Years * RouteBonusPerYear);
    }

    // Обход в ширину по AlliedKingdoms — все транзитивно союзные государства
    // образуют один общий рынок, обрабатываемый ровно один раз за год
    private static List<Kingdom> CollectTradeCluster(Kingdom start, HashSet<Kingdom> visited)
    {
        var cluster = new List<Kingdom>();
        var queue = new Queue<Kingdom>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            cluster.Add(current);

            foreach (var ally in current.AlliedKingdoms)
            {
                if (ally.FallenYear != null || visited.Contains(ally))
                {
                    continue;
                }

                visited.Add(ally);
                queue.Enqueue(ally);
            }
        }

        return cluster;
    }

    // Переносит долю излишка от поселений с положительным запасом к поселениям
    // с отрицательным, пока не покроет дефицит или не иссякнет доступный излишек.
    // Возвращает пары, между которыми реально прошла передача — для усиления TradeRoute
    private static HashSet<(Settlement, Settlement)> Redistribute(
        List<Settlement> settlements,
        Func<Settlement, double> get,
        Action<Settlement, double> set,
        double transferRate,
        World world)
    {
        var tradedPairs = new HashSet<(Settlement, Settlement)>();

        var deficits = settlements.Where(s => get(s) < 0).ToList();
        var donors = settlements.Where(s => get(s) > 0).ToList();

        if (deficits.Count == 0 || donors.Count == 0)
        {
            return tradedPairs;
        }

        foreach (var deficit in deficits)
        {
            var needed = -get(deficit);

            foreach (var donor in donors)
            {
                if (needed <= 0)
                {
                    break;
                }

                var effectiveRate = transferRate * GetRouteFactor(donor, deficit, world);
                var available = get(donor) * effectiveRate;
                var transfer = Math.Min(available, needed);

                if (transfer <= 0)
                {
                    continue;
                }

                set(donor, get(donor) - transfer);
                set(deficit, get(deficit) + transfer);
                needed -= transfer;
                tradedPairs.Add((donor, deficit));
            }
        }

        return tradedPairs;
    }
}
