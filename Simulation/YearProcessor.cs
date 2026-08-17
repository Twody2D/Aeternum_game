using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Simulation;


// Обработка одного года симуляции: фиксированный порядок систем за тик
public static class YearProcessor
{
    public static void Process(World world)
    {
        world.CurrentYear++;

        // 1. Старение людей
        AgeSystem.Process(world);

        // 2. Браки
        MarriageSystem.Process(world);

        // 3. Разводы
        DivorceSystem.Process(world);

        // 4. Проверка смертей
        DeathSystem.Process(world);

        // 5. Еда: производство/потребление, голод при дефиците
        EconomySystem.Process(world);

        // 6. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // 7. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
