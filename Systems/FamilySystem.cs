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


    father.CurrentFamily = family;
    mother.CurrentFamily = family;


    mother.LastName = father.LastName;


    // Работа с династией
        if (father.Dynasty == null)
        {
            var dynasty = DynastySystem.CreateDynasty(
                father,
                world
            );

            mother.Dynasty = dynasty;

            family.Dynasty = dynasty;

            DynastySystem.AddMember(
                dynasty,
                mother
            );
        }
        else
        {
            family.Dynasty = father.Dynasty;

            DynastySystem.AddMember(
                father.Dynasty,
                mother
            );
        }


        world.Families.Add(family);


        return family;
    }
    public static void AddChildToFamily(
        Family family,
        Character child)
    {
        family.Children.Add(child);
        child.BirthFamily = family;
    }
}