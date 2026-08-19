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

        // 5. Опекунство: детям, оставшимся без обоих родителей, подбирается опекун
        OrphanSystem.Process(world);

        // 6. Катастрофы: эпидемии и неурожай (до Economy, чтобы учлись в этом же году)
        DisasterSystem.Process(world);

        // 7. Еда: производство/потребление, голод при дефиците
        EconomySystem.Process(world);

        // 8. Торговля излишками между поселениями одного государства
        TradeSystem.Process(world);

        // 9. Дань в казну государства
        TributeSystem.Process(world);

        // 10. Строительство домов из накопленных материалов
        HousingSystem.Process(world);

        // 11. Строительство больниц из накопленных материалов
        HospitalSystem.Process(world);

        // 12. Миграция из голодающих поселений
        MigrationSystem.Process(world);

        // 13. Колонизация: переполненные поселения основывают новые
        ColonizationSystem.Process(world);

        // 14. Государства: обновление контроля территорий, престолонаследие, новые королевства
        KingdomSystem.Process(world);

        // 15. Союзы между государствами
        AllianceSystem.Process(world);

        // 16. Войны за спорные поселения между государствами
        WarSystem.Process(world);

        // 17. Заговоры против правителя
        MurderSystem.Process(world);

        // 18. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // 19. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
