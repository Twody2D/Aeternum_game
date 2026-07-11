using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;


namespace Aeternum.WorldGen.Systems;


public static class FamilySystem
{

    public static void CreateFamily(
        Character mother,
        Character father,
        Character child,
        World world)
    {

        // Проверяем, есть ли уже семья этих родителей
        var family = world.Families
            .FirstOrDefault(f =>
                f.Mother == mother &&
                f.Father == father);


        // Если семьи нет - создаём
        if (family == null)
        {
            family = new Family
            {
                Mother = mother,
                Father = father
            };


            world.Families.Add(family);
        }


        // Добавляем ребёнка
        family.Children.Add(child);
    }
}