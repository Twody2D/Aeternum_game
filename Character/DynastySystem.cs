using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.GameWorld;

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