using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;


public static class MarriageSystem
{
    private static readonly Random _random = new();


    public static void Process(World world)
    {
        var availableMen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Male &&
                c.Age >= 18 &&
                c.Family == null)
            .ToList();


        var availableWomen = world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Female &&
                c.Age >= 18 &&
                c.Family == null)
            .ToList();



        int couples = Math.Min(
            availableMen.Count,
            availableWomen.Count
        );


        for(int i = 0; i < couples; i++)
        {
            var man = availableMen[i];

            var woman = availableWomen[i];


            // вероятность брака
            if(_random.Next(100) < 50)
            {
                FamilySystem.CreateFamily(
                    man,
                    woman,
                    world
                );


                world.Events.Add(
                    new WorldEvent
                    {
                        Year = world.CurrentYear,

                        Type = EventType.Marriage,

                        Description =
                        $"{man.Name} и {woman.Name} создали семью"
                    }
                );
            }
        }
    }
}