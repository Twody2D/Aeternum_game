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

        // 4. Скандалы: раскрытые измены рвут вражду, а не всегда и сам брак
        ScandalSystem.Process(world);

        // 5. Проверка смертей
        DeathSystem.Process(world);

        // 6. Опекунство: детям, оставшимся без обоих родителей, подбирается опекун
        OrphanSystem.Process(world);

        // 7. Погода года: плавный, общий для всего мира фон, на котором родится урожай
        WeatherSystem.Process(world);

        // 8. Катастрофы: эпидемии и неурожай (до Economy, чтобы учлись в этом же году)
        DisasterSystem.Process(world);

        // 9. Еда: производство/потребление, голод при дефиците
        EconomySystem.Process(world);

        // 10. Торговля излишками между поселениями одного государства
        TradeSystem.Process(world);

        // 11. Языки: поселения перенимают наречие соседей по наезженным путям
        LanguageSystem.Process(world);

        // 12. Дань в казну государства
        TributeSystem.Process(world);

        // 13. Содержание войска: казна платит воинам, иначе они расходятся
        ArmySystem.Process(world);

        // 14. Строительство домов из накопленных материалов
        HousingSystem.Process(world);

        // 15. Строительство больниц из накопленных материалов
        HospitalSystem.Process(world);

        // 16. Строительство мастерских из накопленных материалов
        WorkshopSystem.Process(world);

        // 17. Строительство школ из накопленных материалов
        SchoolSystem.Process(world);

        // 18. Строительство укреплений из накопленных материалов
        WallSystem.Process(world);

        // 19. Рынок: сбыт лишнего и покупка еды за золото (вне союзной сети).
        // После строек — иначе продали бы то, из чего собирались строить
        MarketSystem.Process(world);

        // 20. Хранение: порча еды и потери сверх вместимости складов
        // (последним в экономической фазе — сначала дать потратить, потом отнять лишнее)
        StorageSystem.Process(world);

        // 21. Перемена ремесла: нужда поселения перебивает привычное занятие
        CareerSystem.Process(world);

        // 22. Миграция из голодающих поселений
        MigrationSystem.Process(world);

        // 23. Колонизация: переполненные поселения основывают новые
        ColonizationSystem.Process(world);

        // 24. Государства: обновление контроля территорий, престолонаследие, новые королевства
        KingdomSystem.Process(world);

        // 25. Столица: престол там, где государь
        CapitalSystem.Process(world);

        // 26. Двор: наследник, воевода, казначей и советник при короне
        CourtSystem.Process(world);

        // 27. Союзы между государствами
        AllianceSystem.Process(world);

        // 28. Войны за спорные поселения между государствами
        WarSystem.Process(world);

        // 29. Восстания: поселения отказывают короне в повиновении
        RebellionSystem.Process(world);

        // 30. Расколы веры в общинах, отрезанных от единоверцев
        SchismSystem.Process(world);

        // 31. Накопление знаний и смена эпох
        TechnologySystem.Process(world);

        // 32. Заговоры против правителя
        MurderSystem.Process(world);

        // 33. Рождение детей
        List<Character> newborns = new();

        BirthSystem.ProcessBirths(
            newborns,
            world
        );

        // ...и те, кто родился вне семьи
        BastardSystem.Process(newborns, world);

        // 34. Добавляем детей
        world.Characters.AddRange(newborns);
    }
}
