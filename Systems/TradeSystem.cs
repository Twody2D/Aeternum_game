using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Торговля внутри государства: без карты/географии единственная существующая
// группировка поселений — Kingdom.Settlements. Сглаживает дефицит/излишки еды
// и материалов между провинциями одного государства — государство теперь даёт
// не только политическую, но и экономическую пользу. Ничего не логирует —
// тот же принцип, что и у EconomySystem (перераспределение не событие,
// событие — только заметное последствие вроде голода)
public static class TradeSystem
{
    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null || kingdom.Settlements.Count < 2)
            {
                continue;
            }

            Redistribute(kingdom.Settlements, s => s.FoodStock, (s, v) => s.FoodStock = v, world.Settings.TradeTransferRate);
            Redistribute(kingdom.Settlements, s => s.MaterialStock, (s, v) => s.MaterialStock = v, world.Settings.TradeTransferRate);
        }
    }

    // Переносит долю излишка от поселений с положительным запасом к поселениям
    // с отрицательным, пока не покроет дефицит или не иссякнет доступный излишек
    private static void Redistribute(
        List<Settlement> settlements,
        Func<Settlement, double> get,
        Action<Settlement, double> set,
        double transferRate)
    {
        var deficits = settlements.Where(s => get(s) < 0).ToList();
        var donors = settlements.Where(s => get(s) > 0).ToList();

        if (deficits.Count == 0 || donors.Count == 0)
        {
            return;
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

                var available = get(donor) * transferRate;
                var transfer = Math.Min(available, needed);

                if (transfer <= 0)
                {
                    continue;
                }

                set(donor, get(donor) - transfer);
                set(deficit, get(deficit) + transfer);
                needed -= transfer;
            }
        }
    }
}
