using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;


namespace Aeternum.WorldGen.Systems;


public static class FamilySystem
{

    public static Family CreateFamily(
        Character mother,
        Character father,
        //Character child,
        World world)
    {
        var family = new Family
        {
            Id = world.Families.Count + 1,
            Father = father,
            Mother = mother
        };
        father.Family = family;
        mother.Family = family;


        world.Families.Add(family);


        return family;

        // Проверяем, есть ли уже семья этих родителей
        // var family = world.Families
        //     .FirstOrDefault(f =>
        //         f.Mother == mother &&
        //         f.Father == father);


        // Если семьи нет - создаём
        // if (family == null)
        // {
        //     family = new Family
        //     {
        //         Mother = mother,
        //         Father = father
        //     };


        //     world.Families.Add(family);
  
        // }


        // Добавляем ребёнка
       // family.Children.Add(child);
    }
    public static void AddChildToFamily(
        Family family,
        Character child)
    {
        family.Children.Add(child);
        child.Family = family;
    }
    
        // Проверяем, есть ли уже семья этих родителей
        // var family = world.Families
        //     .FirstOrDefault(f =>
        //         f.Mother == mother &&
        //         f.Father == father);
}