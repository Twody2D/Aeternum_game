using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Save;
using Aeternum.WorldGen.Settings;
using Aeternum.WorldGen.Systems;



Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();
world.AliveCount = ProjectSettings.StartingPopulation; // Инициализируем счетчик живых персонажей
world.Cultures = CultureGenerator.Create(ProjectSettings.SettlementCount);
world.Religions = ReligionGenerator.Create(ProjectSettings.SettlementCount);
world.Settlements = SettlementGenerator.Create(ProjectSettings.SettlementCount);

// У каждого поселения — своя культура и религия. Религия назначается со сдвигом
// относительно культуры, чтобы они не были жёстко связаны 1:1
for (int i = 0; i < world.Settlements.Count; i++)
{
    world.Settlements[i].Culture = world.Cultures[i % world.Cultures.Count];
    world.Settlements[i].Religion = world.Religions[(i + 1) % world.Religions.Count];
}

// Создаём стартовое население случайными взрослыми персонажами,
// равномерно распределяя их между поселениями
for (int i = 0; i < ProjectSettings.StartingPopulation; i++)
{
    var settlement = world.Settlements[i % world.Settlements.Count];
    var character = CharacterGenerator.Create(settlement.Culture);

    character.Settlement = settlement;
    character.BirthYear = world.CurrentYear - character.Age; // Родился до начала отсчёта мира
    settlement.Members.Add(character);

    world.Characters.Add(character);
}

// Русские подписи типов событий для хроники по десятилетиям
var eventLabels = new Dictionary<EventType, string>
{
    [EventType.Birth] = "рождений",
    [EventType.Death] = "смертей",
    [EventType.Marriage] = "свадеб",
    [EventType.Divorce] = "разводов",
    [EventType.Migration] = "переездов",
    [EventType.CreationOfDynasty] = "основано династий",
    [EventType.Colonization] = "основано поселений",
    [EventType.CreationOfKingdom] = "образовано государств",
    [EventType.Succession] = "смен правителя",
    [EventType.Disaster] = "катастроф",
    [EventType.War] = "войн",
    [EventType.FallOfKingdom] = "государств пало",
    [EventType.Murder] = "заговоров против правителя"
};

var ageGroupLabels = new Dictionary<AgeGroup, string>
{
    [AgeGroup.Child] = "Дети",
    [AgeGroup.Adult] = "Взрослые",
    [AgeGroup.Elder] = "Пожилые"
};

PrintInitialPopulation(StatisticsSystem.BuildInitialPopulationReport(world));

var engine = new SimulationEngine();

// Запускаем симуляцию; колбэк вызывается после каждого года и печатает
// заголовок года и все произошедшие в нём события — это чисто консольная
// логика, само ядро (World/YearProcessor) о консоли ничего не знает
engine.Run(world, ProjectSettings.SimulationYears, w =>
{
    Console.WriteLine($"===== Год {w.CurrentYear} =====");

    foreach (var yearEvent in EventSystem.GetYearEvents(w))
    {
        Console.WriteLine(yearEvent.Description);
    }
});

PrintFinalReport(StatisticsSystem.BuildFinalReport(world));
PrintChronicle(ChronicleSystem.BuildChronicle(world));
PrintNotablePeople(NotablePeopleSystem.BuildReport(world));
PrintDynasties(DynastyEncyclopediaSystem.BuildReport(world));
PrintKingdoms(DynastyEncyclopediaSystem.BuildKingdomsReport(world));

const string savePath = "world_save.json";

SaveSystem.Save(world, savePath);
Console.WriteLine($"Мир сохранён в {savePath}");

var loadedWorld = SaveSystem.Load(savePath);
Console.WriteLine(
    $"Проверка загрузки: год {loadedWorld.CurrentYear}, живых {loadedWorld.Characters.Count(c => c.Alive)}, " +
    $"династий {loadedWorld.Dynasties.Count}, семей {loadedWorld.Families.Count}, королевств {loadedWorld.Kingdoms.Count}");

// Список стартовых жителей с возрастом и профессией
void PrintInitialPopulation(List<Character> characters)
{
    Console.WriteLine();
    Console.WriteLine("===== Начальное население =====");

    foreach (var character in characters)
    {
        Console.WriteLine(
            $"{SurnameSystem.GetDisplayFullName(character)}, {character.Age} лет, " +
            $"{character.Profession}, {character.Settlement?.Name}");
    }

    Console.WriteLine();
}

// Итоговая статистика по завершении симуляции: демография и возрастные группы
void PrintFinalReport(WorldStatistics stats)
{
    Console.WriteLine();
    Console.WriteLine("===== Итоги симуляции =====");
    Console.WriteLine($"Возраст мира: {stats.CurrentYear} лет");
    Console.WriteLine($"Всего жителей создано: {stats.TotalCharactersCreated}");
    Console.WriteLine($"Живых персонажей: {stats.AliveCount}");
    Console.WriteLine($"Всего рождений: {stats.TotalBirths}");
    Console.WriteLine($"Всего смертей: {stats.TotalDeaths}");
    Console.WriteLine();

    Console.WriteLine("Поселения:");

    foreach (var settlementStat in stats.Settlements)
    {
        var settlement = settlementStat.Settlement;

        Console.WriteLine(
            $"{settlement.Name} ({settlement.Culture?.Name}, {settlement.Religion?.Name}): " +
            $"{settlementStat.Population} жит., запас еды {settlement.FoodStock:0.#}");
    }

    Console.WriteLine();
    Console.WriteLine("Распределение по возрасту:");

    foreach (var ageGroup in stats.AgeGroups)
    {
        Console.WriteLine($"{ageGroupLabels[ageGroup.Group]}: {ageGroup.Count}");
    }

    Console.WriteLine();
}

// Хроника мира: сводка по десятилетиям вместо построчного вывода каждого события
void PrintChronicle(List<ChroniclePeriod> periods)
{
    Console.WriteLine();
    Console.WriteLine("===== Хроника мира =====");

    foreach (var period in periods)
    {
        var parts = period.Tallies.Select(t => $"{t.Count} {eventLabels[t.Type]}");

        Console.WriteLine($"Годы {period.StartYear}-{period.EndYear}: {string.Join(", ", parts)}");
    }

    Console.WriteLine();
}

void PrintNotablePeople(List<NotablePerson> notable)
{
    Console.WriteLine();
    Console.WriteLine("===== Выдающиеся личности =====");

    if (notable.Count == 0)
    {
        Console.WriteLine("В этой истории не нашлось никого выдающегося.");
    }

    foreach (var entry in notable)
    {
        var reasons = new List<string>();

        if (entry.IsLongLived)
        {
            reasons.Add($"дожил(а) до {entry.Character.Age} лет");
        }

        if (entry.FoundedSignificantDynasty != null)
        {
            reasons.Add(
                $"основал(а) {entry.FoundedSignificantDynasty.Name} " +
                $"({entry.FoundedSignificantDynasty.Members.Count} представителей рода)");
        }

        Console.WriteLine($"{SurnameSystem.GetDisplayFullName(entry.Character)} — {string.Join(", ", reasons)}");
    }

    Console.WriteLine();
}

// Карточки крупнейших династий: основание, статус, число представителей, долгожители
void PrintDynasties(List<DynastyStat> dynastyStats)
{
    Console.WriteLine();
    Console.WriteLine("===== Династии =====");

    foreach (var stat in dynastyStats)
    {
        var dynasty = stat.Dynasty;

        Console.WriteLine();
        Console.WriteLine($"--- {dynasty.Name} ---");
        Console.WriteLine($"Основана: {dynasty.FoundedYear} год, основатель — {SurnameSystem.GetDisplayFullName(dynasty.Founder)}");

        if (stat.ExtinctYear.HasValue)
        {
            Console.WriteLine($"Угасла: {stat.ExtinctYear} год");
        }
        else
        {
            Console.WriteLine($"Ныне живущих представителей: {stat.AliveCount}");
        }

        Console.WriteLine($"Всего представителей за всю историю: {dynasty.Members.Count}");

        if (stat.LongLived.Count > 0)
        {
            var names = string.Join(", ", stat.LongLived.Select(m => $"{SurnameSystem.GetDisplayFullName(m)} ({m.Age})"));
            Console.WriteLine($"Долгожители: {names}");
        }
    }

    Console.WriteLine();
}

// Список государств: правитель, год основания, число подконтрольных поселений
void PrintKingdoms(List<Kingdom> kingdoms)
{
    Console.WriteLine();
    Console.WriteLine("===== Государства =====");

    if (kingdoms.Count == 0)
    {
        Console.WriteLine("За это время в мире не возникло ни одного государства.");
        Console.WriteLine();

        return;
    }

    foreach (var kingdom in kingdoms)
    {
        if (kingdom.FallenYear.HasValue)
        {
            Console.WriteLine(
                $"{kingdom.Name}: основано в {kingdom.FoundedYear} году, пало в {kingdom.FallenYear} году " +
                $"(династия угасла), последний правитель — {SurnameSystem.GetDisplayFullName(kingdom.Ruler)}");

            continue;
        }

        var rulerStatus = kingdom.Ruler.Alive ? "правит" : "правил(а) последним(ей)";

        Console.WriteLine(
            $"{kingdom.Name}: основано в {kingdom.FoundedYear} году, " +
            $"{SurnameSystem.GetDisplayFullName(kingdom.Ruler)} {rulerStatus}, " +
            $"поселений под контролем: {kingdom.Settlements.Count}");
    }

    Console.WriteLine();
}
