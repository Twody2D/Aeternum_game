using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Хроника мира: сжимает весь лог World.Events в сводку по периодам (по
// умолчанию — десятилетиям) — количество событий каждого типа за период,
// без текста. Русские подписи типов строит вызывающий код.
//
// С kingdom вместо мировой сводки выходит летопись одной короны — по тем
// же периодам, но только по событиям, которые её касаются (см. WorldEvent.Kingdoms,
// проставляется в месте создания события: KingdomSystem, WarSystem, AllianceSystem
// и остальные системы политической жизни)
public static class ChronicleSystem
{
    public static List<ChroniclePeriod> BuildChronicle(World world, int periodLength = 10, Kingdom? kingdom = null)
    {
        var events = kingdom == null ? world.Events : world.Events.Where(e => e.Kingdoms.Contains(kingdom));

        var periods = events
            .GroupBy(e => (e.Year - 1) / periodLength)
            .OrderBy(g => g.Key);

        var result = new List<ChroniclePeriod>();

        foreach (var period in periods)
        {
            var startYear = period.Key * periodLength + 1;
            var endYear = startYear + periodLength - 1;

            // GroupBy сохраняет порядок первого появления ключа в периоде, а не
            // фиксированный порядок — без явной сортировки перечисление "N рождений,
            // M смертей, ..." скакало бы от десятилетия к десятилетию. Порядок
            // объявления в EventType и так логически осмысленный (демография →
            // браки → миграция/колонизация → государства → катастрофы/войны)
            var tallies = period
                .GroupBy(e => e.Type)
                .OrderBy(g => (int)g.Key)
                .Select(g => new EventTally(g.Key, g.Count()))
                .ToList();

            result.Add(new ChroniclePeriod(startYear, endYear, tallies));
        }

        return result;
    }
}
