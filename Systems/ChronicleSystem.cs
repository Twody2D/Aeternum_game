using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Хроника мира: сжимает весь лог World.Events в сводку по периодам (по
// умолчанию — десятилетиям) — количество событий каждого типа за период,
// без текста. Русские подписи типов строит вызывающий код
public static class ChronicleSystem
{
    public static List<ChroniclePeriod> BuildChronicle(World world, int periodLength = 10)
    {
        var periods = world.Events
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
