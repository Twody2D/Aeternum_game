using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

public static class DynastySystem
{

    public static Dynasty CreateDynasty(
        Character founder,
        World world)
    {

        var dynasty = new Dynasty
        {
            Name = $"Дом {founder.LastName}"
        };


        dynasty.Members.Add(founder);
        founder.Dynasty = dynasty;

        world.Dynasties.Add(dynasty);

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.CreationOfDynasty,
            Description = $"Основан {dynasty.Name} ({founder.Name} {founder.LastName})"
        });

        return dynasty;
    }



    public static void AddMember(
        Dynasty dynasty,
        Character character)
    {
        dynasty.Members.Add(character);
    }
}
