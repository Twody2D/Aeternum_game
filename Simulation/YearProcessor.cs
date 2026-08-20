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

        // 9. Языки: поселения перенимают наречие соседей по наезженным путям
        LanguageSystem.Process(world);

        // 10. Дань в казну государства
        TributeSystem.Process(world);

        // 11. Содержание войска: казна платит воинам, иначе они расходятся
        ArmySystem.Process(world);

        // 12. Строительство домов из накопленных материалов
        HousingSystem.Process(world);

        // 13. Строительство больниц из накопленных материалов
        HospitalSystem.Process(world);

        // 14. Строительство мастерских из накопленных материалов
        WorkshopSystem.Process(world);

        // 15. Строительство школ из накопленных материалов
        SchoolSystem.Process(world);

        // 16. Строительство укреплений из накопленных материалов
        WallSystem.Process(world);

        // 17. Рынок: сбыт лишнего и покупка еды за золото (вне союзной сети).
        // После строек — иначе продали бы то, из чего собирались строить
        MarketSystem.Process(world);

        // 18. Хранение: порча еды и потери сверх вместимости складов
        // (последним в экономической фазе — сначала дать потратить, потом отнять лишнее)
        StorageSystem.Process(world);

        // 19. Перемена ремесла: нужда поселения перебивает привычное занятие
        CareerSystem.Process(world);

        // 20. Миграция из голодающих поселений
        MigrationSystem.Process(world);

        // 21. Колонизация: переполненные поселения основывают новые
        ColonizationSystem.Process(world);

        // 22. Государства: обновление контроля территорий, престолонаследие, новые королевства
        KingdomSystem.Process(world);

        // 23. Столица: престол там, где государь
        CapitalSystem.Process(world);

        // 24. Двор: наследник, воевода, казначей и советник при короне
        CourtSystem.Process(world);

        // 25. Союзы между государствами
        AllianceSystem.Process(world);

        // 26. Войны за спорные поселения между государствами
        WarSystem.Process(world);

        // 27. Восстания: поселения отказывают короне в повиновении
        RebellionSystem.Process(world);

        // 28. Расколы веры в общинах, отрезанных от единоверцев
        SchismSystem.Process(world);

        // 29. Накопление знаний и смена эпох
        TechnologySystem.Process(world);

        // 30. Заговоры против правителя
        MurderSystem.Process(world);

        // 31. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // ...и те, кто родился вне семьи
        BastardSystem.Process(newborns, world);

        // 32. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
