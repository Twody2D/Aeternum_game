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

        // 9. Рынок: покупка еды за золото при дефиците (вне союзной сети)
        MarketSystem.Process(world);

        // 10. Дань в казну государства
        TributeSystem.Process(world);

        // 11. Строительство домов из накопленных материалов
        HousingSystem.Process(world);

        // 12. Строительство больниц из накопленных материалов
        HospitalSystem.Process(world);

        // 13. Строительство мастерских из накопленных материалов
        WorkshopSystem.Process(world);

        // 14. Строительство школ из накопленных материалов
        SchoolSystem.Process(world);

        // 15. Строительство укреплений из накопленных материалов
        WallSystem.Process(world);

        // 16. Миграция из голодающих поселений
        MigrationSystem.Process(world);

        // 17. Колонизация: переполненные поселения основывают новые
        ColonizationSystem.Process(world);

        // 18. Государства: обновление контроля территорий, престолонаследие, новые королевства
        KingdomSystem.Process(world);

        // 19. Союзы между государствами
        AllianceSystem.Process(world);

        // 20. Войны за спорные поселения между государствами
        WarSystem.Process(world);

        // 21. Восстания: поселения отказывают короне в повиновении
        RebellionSystem.Process(world);

        // 22. Заговоры против правителя
        MurderSystem.Process(world);

        // 23. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // 24. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
