using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

public static class EventSystem
{
    public static void PrintYearEvents(World world)
    {
        var yearEvents = world.Events
            .Where(e => e.Year == world.CurrentYear)
            .ToList();

        if (yearEvents.Count == 0)
        {
            return;
        }

        foreach (var worldEvent in yearEvents)
        {
            Console.WriteLine(worldEvent.Description);
        }
    }
}