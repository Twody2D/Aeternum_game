using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

public static class EventSystem
{
    public static List<WorldEvent> GetYearEvents(World world)
    {
        return world.Events
            .Where(e => e.Year == world.CurrentYear)
            .ToList();
    }
}
