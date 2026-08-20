using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Столица. У государства не было главного города: все его земли были
// одинаково близки короне, и приказ доходил до дальнего хутора так же
// исправно, как до соседней улицы.
//
// Столица не назначается отдельным решением: при основании государства ею
// становится город правителя, а дальше престол стоит на месте, пока стоит
// сам город. Переносят его только вынужденно — когда столица потеряна,
// отпала или вымерла. Сперва столицей был просто нынешний дом государя,
// но замер показал, что тогда престол кочует по 3-5 раз за десятилетие,
// следуя за каждой сменой правителя: престол — институция, а не адрес.
//
// Отсюда берётся то, ради чего всё и заводилось, — тирания расстояния.
// Чем дальше земля от столицы, тем хуже она управляется: дань доходит
// неполной (см. TributeSystem), а недовольство копится быстрее
// (см. RebellionSystem). Никаких новых данных для этого не нужно —
// у поселений давно есть координаты (см. ClimateSystem)
public static class CapitalSystem
{
    // Насколько хуже управляется земля на другом конце карты. Полная потеря
    // управления не наступает никогда: даже дальняя окраина остаётся частью
    // государства, просто обходится ему дороже
    private const double MaxDistancePenalty = 0.5;

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            var seat = ChooseCapital(kingdom);

            if (seat == null || seat == kingdom.Capital)
            {
                continue;
            }

            var previous = kingdom.Capital;
            kingdom.Capital = seat;

            if (previous == null)
            {
                continue; // Первая столица — не перенос, а само основание
            }

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.CapitalMoved,
                Description = $"{kingdom.Name}: престол перенесён из {previous.Name} в {seat.Name}"
            });
        }
    }

    // Прежний престол держится, пока город принадлежит государству и жив.
    // Новый выбирается только если держаться уже не за что: сперва дом
    // нынешнего государя, а если и он не годится — самый людный из оставшихся
    private static Settlement? ChooseCapital(Kingdom kingdom)
    {
        if (IsViable(kingdom, kingdom.Capital))
        {
            return kingdom.Capital;
        }

        if (IsViable(kingdom, kingdom.Ruler.Settlement))
        {
            return kingdom.Ruler.Settlement;
        }

        return kingdom.Settlements
            .Where(s => s.Members.Any(m => m.Alive))
            .OrderByDescending(s => s.Members.Count(m => m.Alive))
            .ThenBy(s => s.Id)
            .FirstOrDefault();
    }

    private static bool IsViable(Kingdom kingdom, Settlement? settlement)
    {
        return settlement != null
               && kingdom.Settlements.Contains(settlement)
               && settlement.Members.Any(m => m.Alive);
    }

    // Насколько крепко корона держит эту землю: 1.0 у самой столицы,
    // тем меньше, чем она дальше
    public static double GetControl(Kingdom kingdom, Settlement settlement)
    {
        if (kingdom.Capital == null || kingdom.Capital == settlement)
        {
            return 1.0;
        }

        var dx = kingdom.Capital.X - settlement.X;
        var dy = kingdom.Capital.Y - settlement.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Мера расстояния — та же карта, по которой считается плодородие
        var share = Math.Min(1.0, distance / ClimateSystem.MapSize);

        return 1 - MaxDistancePenalty * share;
    }
}
