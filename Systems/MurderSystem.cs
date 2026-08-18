using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Заговоры против правителя: если у правящей династии есть другой живой
// взрослый представитель, помимо самого правителя, — настоящий потенциальный
// соперник за трон — с небольшим шансом в год правитель гибнет от заговора.
// Кто унаследует трон дальше — не решается здесь: обычная логика KingdomSystem
// подберёт старшего живого члена династии в следующем году, заговорщику это
// не гарантировано
public static class MurderSystem
{
    private static readonly Random _random = new();

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null || !kingdom.Ruler.Alive)
            {
                continue;
            }

            var rivals = kingdom.Dynasty.Members
                .Where(m => m.Alive && m != kingdom.Ruler && m.Age >= world.Settings.AdultAge)
                .ToList();

            if (rivals.Count == 0)
            {
                continue; // Соперничать некому — заговору не из чего родиться
            }

            if (_random.NextDouble() >= world.Settings.RegicideChance)
            {
                continue;
            }

            var rival = rivals[_random.Next(rivals.Count)];
            var ruler = kingdom.Ruler;

            DeathSystem.Kill(ruler, world, DeathReason.Murder);

            // Label-формат ("{Королевство}: ...") вместо склонения названия
            // государства — та же договорённость, что у DisasterSystem/WarSystem
            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Murder,
                Description = $"{kingdom.Name}: {SurnameSystem.GetDisplayFullName(ruler)} убит(а) в результате заговора. " +
                              $"Подозревают {SurnameSystem.GetDisplayFullName(rival)}"
            });
        }
    }
}
