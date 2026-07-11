using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

public static class DynastySystem
{

    public static Dynasty CreateDynasty(
        Character founder,
        World world)
    {

        var dynasty = new Dynasty
        {
            Name = $"Дом {founder.Name}"
        };


        dynasty.Members.Add(founder);


        world.Dynasties.Add(dynasty);


        return dynasty;
    }



    public static void AddMember(
        Dynasty dynasty,
        Character character)
    {
        dynasty.Members.Add(character);
    }
}