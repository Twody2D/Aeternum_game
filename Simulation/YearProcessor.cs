using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Simulation;


public static class YearProcessor
{
    public static void Process(World world)
    {
        world.CurrentYear++;

        Console.WriteLine(
            $"===== Год {world.CurrentYear} ====="
        );


        // 1. Старение людей
        AgeSystem.Process(world);

        MarriageSystem.Process(world);


        // 2. Проверка смертей
        DeathSystem.Process(world);


        // 3. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );


        // 4. Добавляем новых жителей
        world.Characters.AddRange(newborns);
    }
}