using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Союзы между государствами: без карты/географии единственный существующий
// сигнал для сближения — тот же, что уже используется в MarriageSystem для
// межпоселенческих браков, только в обратную сторону: общая религия правящих
// домов (Kingdom не хранит свою религию — берём религию поселения, где живёт
// действующий правитель). Союз здесь не разрывается — это может стать
// отдельным следующим шагом
public static class AllianceSystem
{
    private static readonly Random _random = new();

    public static void Process(World world)
    {
        var activeKingdoms = world.Kingdoms.Where(k => k.FallenYear == null).ToList();

        for (var i = 0; i < activeKingdoms.Count; i++)
        {
            for (var j = i + 1; j < activeKingdoms.Count; j++)
            {
                TryFormAlliance(activeKingdoms[i], activeKingdoms[j], world);
            }
        }
    }

    private static void TryFormAlliance(Kingdom a, Kingdom b, World world)
    {
        if (a.AlliedKingdoms.Contains(b))
        {
            return; // Уже союзники
        }

        var religionA = a.Ruler.Settlement?.Religion;
        var religionB = b.Ruler.Settlement?.Religion;

        if (religionA == null || religionA != religionB)
        {
            return;
        }

        if (_random.NextDouble() >= world.Settings.AllianceChance)
        {
            return;
        }

        a.AlliedKingdoms.Add(b);
        b.AlliedKingdoms.Add(a);

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Alliance,
            Description = $"{a.Name} и {b.Name} заключили союз на почве общей веры"
        });
    }
}
