using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;


namespace Aeternum.WorldGen.Systems;


// Создание семей и добавление в них детей: правила фамилии и наследования династии
public static class FamilySystem
{
    private static readonly Random _random = new();

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
        Mother = mother,
        FormedYear = world.CurrentYear
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

            // Dynasty матери не трогаем — как и у отца, брак не переписывает
            // её родной дом (см. AddChildToFamily, где это используется)
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
        Character child,
        World world)
    {
        family.Children.Add(child);
        child.BirthFamily = family;

        var dynasty = family.Dynasty;

        // Материнская ветвь: с небольшим шансом ребёнок идёт по родному дому
        // матери вместо дома отца, если он у неё есть и отличается от отцовского —
        // противовес тому, что новый дом основывается практически только в первом
        // поколении браков (см. CreateFamily), из-за чего 1-2 дома иначе съедают
        // почти всё население за несколько поколений
        var mother = family.Mother;

        if (mother.Dynasty != null &&
            mother.Dynasty != dynasty &&
            _random.NextDouble() < world.Settings.MaternalDynastyChance)
        {
            dynasty = mother.Dynasty;
        }

        if (dynasty != null)
        {
            child.Dynasty = dynasty;

            DynastySystem.AddMember(
                dynasty,
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
