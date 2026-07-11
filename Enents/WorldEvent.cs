using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Events;


public class WorldEvent
{
    public int Year { get; set; }

    public EventType Type { get; set; }

    public string Description { get; set; } = "";
}