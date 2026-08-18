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

        // 5. Катастрофы: эпидемии и неурожай (до Economy, чтобы учлись в этом же году)
        DisasterSystem.Process(world);

        // 6. Еда: производство/потребление, голод при дефиците
        EconomySystem.Process(world);

        // 7. Миграция из голодающих поселений
        MigrationSystem.Process(world);

        // 8. Колонизация: переполненные поселения основывают новые
        ColonizationSystem.Process(world);

        // 9. Государства: обновление контроля территорий, престолонаследие, новые королевства
        KingdomSystem.Process(world);

        // 10. Войны за спорные поселения между государствами
        WarSystem.Process(world);

        // 11. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // 12. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
