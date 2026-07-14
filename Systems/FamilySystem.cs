using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;


namespace Aeternum.WorldGen.Systems;


public static class FamilySystem
{

    public static Family CreateFamily(
        Character mother,
        Character father,
        World world)
    {
        var family = new Family
        {
            Id = world.Families.Count + 1,
            Father = father,
            Mother = mother
        };
        //Фамилия семьи идёт от отца
        mother.LastName = father.LastName;

        father.Family = family;
        mother.Family = family;

        world.Families.Add(family);


        return family;
    }
    public static void AddChildToFamily(
        Family family,
        Character child)
    {
        family.Children.Add(child);
        child.Family = family;
    }
}