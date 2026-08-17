using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;


namespace Aeternum.WorldGen.Systems;


// Создание семей и добавление в них детей: правила фамилии и наследования династии
public static class FamilySystem
{
    private static int _nextFamilyId = 1;

    // Женит father и mother: жена берёт фамилию мужа, семья наследует или основывает династию
    public static Family CreateFamily(
    Character mother,
    Character father,
    World world)
{
    var family = new Family
    {
        Id = _nextFamilyId++,
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

        family.Dynasty!.Families.Add(family);


        world.Families.Add(family);


        return family;
    }
    // Привязывает ребёнка к семье рождения и сразу — к её династии
    public static void AddChildToFamily(
        Family family,
        Character child)
    {
        family.Children.Add(child);
        child.BirthFamily = family;

        if (family.Dynasty != null)
        {
            child.Dynasty = family.Dynasty;

            DynastySystem.AddMember(
                family.Dynasty,
                child
            );
        }
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextFamilyId(int value)
    {
        _nextFamilyId = value;
    }
}
