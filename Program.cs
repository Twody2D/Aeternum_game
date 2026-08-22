using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Export;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Save;
using Aeternum.WorldGen.Settings;
using Aeternum.WorldGen.Systems;



Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

if (!ProjectSettings.Apply(args, Console.Out))
{
    return; // Запрошена справка либо аргументы неверны — мир не создаём
}

// Зерно задаётся до первого обращения к случайности — иначе часть мира успела бы
// родиться на старом генераторе (см. Rng)
Rng.Initialize(ProjectSettings.Seed);
Console.WriteLine($"Зерно мира: {Rng.Seed}");

var world = new World();
world.Seed = Rng.Seed;
world.AliveCount = ProjectSettings.StartingPopulation; // Инициализируем счетчик живых персонажей
world.Cultures = CultureGenerator.Create(ProjectSettings.SettlementCount);
world.Religions = ReligionGenerator.Create(ProjectSettings.SettlementCount);
world.Settlements = SettlementGenerator.Create(ProjectSettings.SettlementCount);
world.Languages = LanguageGenerator.Create(world.Cultures.Count);

// У каждого поселения — своя культура и религия. Религия назначается со сдвигом
// относительно культуры, чтобы они не были жёстко связаны 1:1
for (int i = 0; i < world.Settlements.Count; i++)
{
    world.Settlements[i].Culture = world.Cultures[i % world.Cultures.Count];

    // Наречие идёт по культуре, но их меньше, чем народов: родственные культуры
    // достаются одному языку, и языковая граница не совпадает с культурной
    world.Settlements[i].Language = world.Languages[(i % world.Cultures.Count) % world.Languages.Count];
    world.Settlements[i].Religion = world.Religions[(i + 1) % world.Religions.Count];
}

// Создаём стартовое население случайными взрослыми персонажами,
// равномерно распределяя их между поселениями
for (int i = 0; i < ProjectSettings.StartingPopulation; i++)
{
    var settlement = world.Settlements[i % world.Settlements.Count];
    var character = CharacterGenerator.Create(settlement.Culture, settlement);

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
    [EventType.Murder] = "заговоров против правителя",
    [EventType.Alliance] = "союзов",
    [EventType.CivilWar] = "гражданских войн",
    [EventType.Guardianship] = "взятий под опеку",
    [EventType.AllianceBroken] = "разорванных союзов",
    [EventType.Peace] = "перемирий",
    [EventType.Vassalization] = "признаний вассалитета",
    [EventType.Rebellion] = "восстаний",
    [EventType.Schism] = "расколов веры",
    [EventType.Era] = "смен эпохи",
    [EventType.Suppression] = "подавленных мятежей",
    [EventType.Independence] = "обретений независимости",
    [EventType.Relief] = "раздач хлеба",
    [EventType.Appointment] = "назначений ко двору",
    [EventType.Assimilation] = "смен наречия",
    [EventType.Captivity] = "угонов пленных",
    [EventType.CapitalMoved] = "переносов престола",
    [EventType.Desertion] = "роспусков войска",
    [EventType.Weather] = "приметных погодных лет",
    [EventType.Scandal] = "раскрытых измен",
    [EventType.Espionage] = "успешных интриг"
};

var ageGroupLabels = new Dictionary<AgeGroup, string>
{
    [AgeGroup.Child] = "Дети",
    [AgeGroup.Adult] = "Взрослые",
    [AgeGroup.Elder] = "Пожилые"
};

var deathReasonLabels = new Dictionary<DeathReason, string>
{
    [DeathReason.OldAge] = "от старости",
    [DeathReason.Disease] = "от болезни",
    [DeathReason.Accident] = "от несчастного случая",
    [DeathReason.War] = "на войне",
    [DeathReason.Starvation] = "от голода",
    [DeathReason.Murder] = "будучи убит(а)",
    [DeathReason.Neglect] = "оставшись без присмотра"
};

var materialLabels = new Dictionary<MaterialType, string>
{
    [MaterialType.Wood] = "дерево",
    [MaterialType.Stone] = "камень",
    [MaterialType.Metal] = "металл",
    [MaterialType.Textile] = "ткани",
    [MaterialType.Clay] = "утварь",
    [MaterialType.Luxury] = "роскошь"
};

var traitLabels = new Dictionary<Trait, string>
{
    [Trait.Hardworking] = "трудолюбивый(ая)",
    [Trait.Frail] = "слабое здоровье",
    [Trait.Brave] = "смелый(ая)",
    [Trait.Prudent] = "осторожный(ая)"
};

PrintInitialPopulation(StatisticsSystem.BuildInitialPopulationReport(world));

var engine = new SimulationEngine();

// Запускаем симуляцию; колбэк вызывается после каждого года и печатает
// заголовок года и все произошедшие в нём события — это чисто консольная
// логика, само ядро (World/YearProcessor) о консоли ничего не знает
engine.Run(world, ProjectSettings.SimulationYears, w =>
{
    if (ProjectSettings.Quiet)
    {
        return; // Длинный прогон смотрим по итогам, а не по каждому году
    }

    Console.WriteLine($"===== Год {w.CurrentYear} =====");

    foreach (var yearEvent in EventSystem.GetYearEvents(w))
    {
        Console.WriteLine(yearEvent.Description);
    }
});

PrintFinalReport(StatisticsSystem.BuildFinalReport(world), world.Knowledge);
PrintWorldMap(WorldMapSystem.Build(world));
PrintChronicle("Хроника мира", ChronicleSystem.BuildChronicle(world));
PrintKingdomChronicles(world);
var notablePeople = NotablePeopleSystem.BuildReport(world);
PrintNotablePeople(notablePeople);
PrintBiographies(notablePeople, world);
PrintDynasties(DynastyEncyclopediaSystem.BuildReport(world));
PrintKingdoms(DynastyEncyclopediaSystem.BuildKingdomsReport(world));

const string savePath = "world_save.json";

SaveSystem.Save(world, savePath);
Console.WriteLine($"Мир сохранён в {savePath}");

var loadedWorld = SaveSystem.Load(savePath);
Console.WriteLine(
    $"Проверка загрузки: год {loadedWorld.CurrentYear}, живых {loadedWorld.Characters.Count(c => c.Alive)}, " +
    $"династий {loadedWorld.Dynasties.Count}, семей {loadedWorld.Families.Count}, королевств {loadedWorld.Kingdoms.Count}");

// Снимок для клиента-отрисовщика — отдельно от сохранения, см. WorldSnapshot
const string snapshotPath = "world_snapshot.json";

SnapshotExporter.Export(world, snapshotPath);
Console.WriteLine($"Снимок для отрисовки: {snapshotPath}");

// Список стартовых жителей с возрастом и профессией
void PrintInitialPopulation(List<Character> characters)
{
    Console.WriteLine();
    Console.WriteLine("===== Начальное население =====");

    foreach (var character in characters)
    {
        var traitsText = character.Traits.Count > 0
            ? $" ({string.Join(", ", character.Traits.Select(t => traitLabels[t]))})"
            : "";

        Console.WriteLine(
            $"{SurnameSystem.GetDisplayFullName(character)}, {character.Age} лет, " +
            $"{character.Profession}, {character.Settlement?.Name}{traitsText}");
    }

    Console.WriteLine();
}

// Итоговая статистика по завершении симуляции: демография и возрастные группы
void PrintFinalReport(WorldStatistics stats, double knowledge)
{
    Console.WriteLine();
    Console.WriteLine("===== Итоги симуляции =====");
    Console.WriteLine($"Возраст мира: {stats.CurrentYear} лет");
    Console.WriteLine($"Эпоха: {TechnologySystem.GetEraName(knowledge)} (знание {knowledge:0})");
    Console.WriteLine($"Всего жителей создано: {stats.TotalCharactersCreated}");
    Console.WriteLine($"Живых персонажей: {stats.AliveCount}");
    Console.WriteLine($"Всего рождений: {stats.TotalBirths}");
    Console.WriteLine($"Всего смертей: {stats.TotalDeaths}");
    Console.WriteLine();

    Console.WriteLine("Поселения:");

    foreach (var settlementStat in stats.Settlements)
    {
        var settlement = settlementStat.Settlement;

        var materialsText = settlement.MaterialStocks.Count(kv => kv.Value > 0) > 0
            ? string.Join(", ", settlement.MaterialStocks.Where(kv => kv.Value > 0).Select(kv => $"{materialLabels[kv.Key]} {kv.Value:0.#}"))
            : "нет";

        var workshopsText = settlement.Workshops.Count(kv => kv.Value > 0) > 0
            ? string.Join(", ", settlement.Workshops.Where(kv => kv.Value > 0).Select(kv => $"{materialLabels[kv.Key]} {kv.Value}"))
            : "нет";

        var legendText = settlement.LegendCount > 0 ? $", легенд {settlement.LegendCount}" : "";
        var spiritualHead = ClergySystem.GetSpiritualHead(settlement, world);
        var clergyText = spiritualHead != null ? $", пастырь {SurnameSystem.GetDisplayFullName(spiritualHead)}" : "";

        Console.WriteLine(
            $"{settlement.Name} ({settlement.Culture?.Name}, {settlement.Religion?.Name}{clergyText}, {settlement.Language?.Name}) " +
            $"[{settlement.X:0}, {settlement.Y:0}, плодородие {ClimateSystem.GetFertility(settlement):0.00}, {TerrainSystem.GetName(TerrainSystem.GetRelief(settlement, world))}]: " +
            $"{settlementStat.Population} жит., домов {settlement.Houses}, больниц {settlement.Hospitals}, " +
            $"школ {settlement.Schools}, укреплений {settlement.Walls}, " +
            $"запас еды {settlement.FoodStock:0.#}/{StorageSystem.GetFoodCapacity(settlement):0}, золота {settlement.Gold:0.#}, материалы: {materialsText}, мастерские: {workshopsText}{legendText}");
    }

    Console.WriteLine();

    var estates = world.Characters
        .Where(c => c.Alive)
        .GroupBy(c => EstateSystem.GetEstate(c, world))
        .OrderBy(g => g.Key)
        .Select(g => $"{EstateSystem.GetName(g.Key)} {g.Count()}");

    Console.WriteLine($"Сословия: {string.Join(", ", estates)}");

    Console.WriteLine();
    Console.WriteLine("Распределение по возрасту:");

    foreach (var ageGroup in stats.AgeGroups)
    {
        Console.WriteLine($"{ageGroupLabels[ageGroup.Group]}: {ageGroup.Count}");
    }

    Console.WriteLine();
}

// ASCII-карта мира: фон — рельеф (см. TerrainSystem), поверх — поселения,
// подписанные буквой государства, которому заметнее прочих принадлежат
void PrintWorldMap(WorldMap map)
{
    Console.WriteLine();
    Console.WriteLine("===== Карта мира =====");

    var kingdoms = map.Cells
        .SelectMany(row => row)
        .Select(c => c.Owner)
        .Where(k => k != null)
        .Select(k => k!)
        .Distinct()
        .OrderBy(k => k.Id)
        .ToList();

    var symbolByKingdom = kingdoms
        .Select((k, i) => (Kingdom: k, Symbol: (char)('A' + i % 26)))
        .ToDictionary(t => t.Kingdom, t => t.Symbol);

    for (var row = 0; row < map.Height; row++)
    {
        var line = new System.Text.StringBuilder();

        for (var col = 0; col < map.Width; col++)
        {
            var cell = map.Cells[row][col];

            char symbol;

            if (cell.Settlement != null)
            {
                symbol = cell.Owner != null && symbolByKingdom.TryGetValue(cell.Owner, out var kingdomSymbol) ? kingdomSymbol : '*';
            }
            else
            {
                symbol = cell.Relief switch
                {
                    Relief.Mountain => '^',
                    Relief.Hill => ',',
                    Relief.Coast => '~',
                    _ => '.'
                };
            }

            line.Append(symbol);
        }

        Console.WriteLine(line.ToString());
    }

    Console.WriteLine();
    Console.WriteLine(". низина   , холмы   ^ горы   ~ побережье/река   * поселение вне государств");

    foreach (var (kingdom, symbol) in symbolByKingdom)
    {
        Console.WriteLine($"{symbol} — {kingdom.Name}");
    }

    Console.WriteLine();
}

// Хроника: сводка по десятилетиям вместо построчного вывода каждого события.
// Тот же вывод обслуживает и мировую хронику, и летопись одной короны — разница
// только в заголовке и в том, какой список периодов ей передали (см. ChronicleSystem)
void PrintChronicle(string title, List<ChroniclePeriod> periods)
{
    Console.WriteLine();
    Console.WriteLine($"===== {title} =====");

    if (periods.Count == 0)
    {
        Console.WriteLine("Ничего примечательного не случилось.");
    }

    foreach (var period in periods)
    {
        var parts = period.Tallies.Select(t => $"{t.Count} {eventLabels[t.Type]}");

        Console.WriteLine($"Годы {period.StartYear}-{period.EndYear}: {string.Join(", ", parts)}");
    }

    Console.WriteLine();
}

// Летопись каждой короны, когда-либо возникавшей в мире, по тем же событиям,
// что и мировая хроника, — только отфильтрованным по тому, кого они касались
// (см. WorldEvent.Kingdoms). Павшие государства получают летопись наравне
// с живыми — история государства не кончается вместе с его гибелью
void PrintKingdomChronicles(World world)
{
    foreach (var kingdom in world.Kingdoms.OrderBy(k => k.FoundedYear))
    {
        var span = kingdom.FallenYear.HasValue
            ? $"{kingdom.FoundedYear}-{kingdom.FallenYear} гг."
            : $"с {kingdom.FoundedYear} года";

        PrintChronicle($"Летопись: {kingdom.Name} ({span})", ChronicleSystem.BuildChronicle(world, kingdom: kingdom));
    }
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

// Биография собирает уже рассыпанные по миру факты (см. BiographySystem) в один
// связный текст — та же выборка личностей, что и в PrintNotablePeople
void PrintBiographies(List<NotablePerson> notable, World world)
{
    Console.WriteLine();
    Console.WriteLine("===== Биографии выдающихся личностей =====");

    if (notable.Count == 0)
    {
        Console.WriteLine();
        return;
    }

    foreach (var entry in notable)
    {
        Console.WriteLine();
        Console.WriteLine(BuildBiographyText(BiographySystem.Build(entry.Character, world), world));
    }

    Console.WriteLine();
}

string BuildBiographyText(Biography bio, World world)
{
    var character = bio.Character;
    var isFemale = character.Gender == Gender.Female;
    var sentences = new List<string>();

    var bornVerb = isFemale ? "родилась" : "родился";
    var parents = character.Mother != null || character.Father != null
        ? $", в семье {(character.Father != null ? SurnameSystem.GetDisplayFullName(character.Father) : "неизвестного отца")}" +
          $" и {(character.Mother != null ? SurnameSystem.GetDisplayFullName(character.Mother) : "неизвестной матери")}"
        : "";
    var bastardNote = BastardSystem.IsBastard(character) ? ", рождён(а) вне брака" : "";

    sentences.Add($"{SurnameSystem.GetDisplayFullName(character)} {bornVerb} в {character.BirthYear} году{parents}{bastardNote}.");

    var estate = EstateSystem.GetName(EstateSystem.GetEstate(character, world));
    var professionText = character.Profession != null ? $" Занимается ремеслом «{character.Profession}»." : "";
    sentences.Add($"Сословие — {estate}.{professionText}");

    foreach (var marriage in bio.Marriages)
    {
        var marryVerb = isFemale ? "вышла замуж за" : "женился на";
        var spouseName = SurnameSystem.GetDisplayFullName(marriage.Spouse);

        var outcome = marriage.Status == MarriageStatus.Current
            ? ""
            : marriage.Spouse.Alive
                ? "; позднее расстались"
                : $"; {(marriage.Spouse.Gender == Gender.Female ? "она" : "он")} умер(ла) в {marriage.Spouse.DeathYear} году";

        sentences.Add($"В {marriage.FormedYear} году {marryVerb} {spouseName}{outcome}.");
    }

    if (bio.Children.Count > 0)
    {
        sentences.Add($"Дети: {string.Join(", ", bio.Children.Select(SurnameSystem.GetDisplayFullName))}.");
    }

    if (bio.Wards.Count > 0)
    {
        sentences.Add($"Взял(а) под опеку: {string.Join(", ", bio.Wards.Select(SurnameSystem.GetDisplayFullName))}.");
    }

    if (bio.RulesKingdom != null)
    {
        sentences.Add($"Правит государством {bio.RulesKingdom.Name}.");
    }

    if (bio.Office != null && bio.OfficeKingdom != null)
    {
        sentences.Add($"Служит {CourtSystem.GetTitle(bio.Office.Value)} при дворе {bio.OfficeKingdom.Name}.");
    }

    if (!character.Alive)
    {
        var reason = deathReasonLabels.GetValueOrDefault(character.DeathReason, "по неизвестной причине");
        var diedVerb = isFemale ? "умерла" : "умер";

        sentences.Add($"{(isFemale ? "Она" : "Он")} {diedVerb} в {character.DeathYear} году в возрасте {character.Age} лет ({reason}).");
    }

    return string.Join(" ", sentences);
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

        var alliesText = kingdom.AlliedKingdoms.Count > 0
            ? $", союзники: {string.Join(", ", kingdom.AlliedKingdoms.Select(k => k.Name))}"
            : "";

        var capitalText = kingdom.Capital != null ? $", престол в {kingdom.Capital.Name}" : "";

        var taxText = $", дань {kingdom.TributeRate:P0}";

        var courtText = kingdom.Court.Count > 0
            ? ", двор: " + string.Join(", ", kingdom.Court
                .Where(kv => kv.Value.Alive)
                .Select(kv => $"{CourtSystem.GetTitle(kv.Key)} {SurnameSystem.GetDisplayFullName(kv.Value)}"))
            : "";

        var treasuryMaterials = kingdom.MaterialTreasury.Where(kv => kv.Value > 0).ToList();

        var treasuryText = kingdom.FoodTreasury > 0 || kingdom.GoldTreasury > 0 || treasuryMaterials.Count > 0
            ? $", казна: {kingdom.FoodTreasury:0.#}/{StorageSystem.GetTreasuryCapacity(kingdom):0} еды, {kingdom.GoldTreasury:0.#} золота" +
              (treasuryMaterials.Count > 0
                  ? $", {string.Join(", ", treasuryMaterials.Select(kv => $"{materialLabels[kv.Key]} {kv.Value:0.#}"))}"
                  : "")
            : "";

        Console.WriteLine(
            $"{kingdom.Name}: основано в {kingdom.FoundedYear} году, " +
            $"{SurnameSystem.GetDisplayFullName(kingdom.Ruler)} {rulerStatus}, " +
            $"поселений под контролем: {kingdom.Settlements.Count}{capitalText}{taxText}{alliesText}{treasuryText}{courtText}");
    }

    Console.WriteLine();
}
