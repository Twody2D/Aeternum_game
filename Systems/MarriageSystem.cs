using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;


// Заключение браков раз в год: подбор пар среди холостых взрослых
public static class MarriageSystem
{
    private static readonly Random _random = new();


    public static void Process(World world)
    {
        // Холостые мужчины и женщины подходящего возраста, порядок перемешан случайно
        var availableMen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Male &&
                c.Age >= world.Settings.AdultAge &&
                c.Age <= 60 &&
                c.CurrentFamily == null)
            .OrderBy(x => _random.Next())
            .ToList();


        var availableWomen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Female &&
                c.Age >= world.Settings.AdultAge &&
                c.Age <= 45 &&
                c.CurrentFamily == null)
            .OrderBy(x => _random.Next())
            .ToList();


        var takenWomen = new HashSet<Character>();

        foreach (var man in availableMen)
        {
            var woman = availableWomen.FirstOrDefault(w =>
                !takenWomen.Contains(w) &&
                !AreRelated(man, w));

            if (woman == null)
            {
                continue;
            }

            // Считаем пару сформированной вне зависимости от исхода броска,
            // чтобы один и тот же человек не участвовал в нескольких парах за год
            takenWomen.Add(woman);

            // вероятность брака
            if (_random.Next(100) >= 50)
            {
                continue;
            }

            FamilySystem.CreateFamily(
                woman,
                man,
                world
            );


            world.Events.Add(
                new WorldEvent
                {
                    Year = world.CurrentYear,

                    Type = EventType.Marriage,

                    Description =
                    $"{SurnameSystem.GetDisplayFullName(man)} и {SurnameSystem.GetDisplayFullName(woman)} создали семью"
                }
            );
        }
    }

    // Запрет браков между близкими родственниками: родитель/ребёнок или общий родитель (братья/сёстры)
    private static bool AreRelated(Character a, Character b)
    {
        if (a.Mother == b || a.Father == b || b.Mother == a || b.Father == a)
        {
            return true;
        }

        if (a.Mother != null && (a.Mother == b.Mother || a.Mother == b.Father))
        {
            return true;
        }

        if (a.Father != null && (a.Father == b.Mother || a.Father == b.Father))
        {
            return true;
        }

        return false;
    }
}
