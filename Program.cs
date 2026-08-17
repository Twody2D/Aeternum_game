using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Save;
using Aeternum.WorldGen.Settings;
using Aeternum.WorldGen.Systems;



Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();
world.AliveCount = ProjectSettings.StartingPopulation; // Инициализируем счетчик живых персонажей
world.Cultures = CultureGenerator.Create(ProjectSettings.SettlementCount);
world.Settlements = SettlementGenerator.Create(ProjectSettings.SettlementCount);

// У каждого поселения — своя культура
for (int i = 0; i < world.Settlements.Count; i++)
{
    world.Settlements[i].Culture = world.Cultures[i % world.Cultures.Count];
}

// Создаём стартовое население случайными взрослыми персонажами,
// равномерно распределяя их между поселениями
for (int i = 0; i < ProjectSettings.StartingPopulation; i++)
{
    var settlement = world.Settlements[i % world.Settlements.Count];
    var character = CharacterGenerator.Create(settlement.Culture);

    character.Settlement = settlement;
    settlement.Members.Add(character);

    world.Characters.Add(character);
}

PrintLines(StatisticsSystem.BuildInitialPopulationReport(world));

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

PrintLines(StatisticsSystem.BuildFinalReport(world));

const string savePath = "world_save.json";

SaveSystem.Save(world, savePath);
Console.WriteLine($"Мир сохранён в {savePath}");

var loadedWorld = SaveSystem.Load(savePath);
Console.WriteLine(
    $"Проверка загрузки: год {loadedWorld.CurrentYear}, живых {loadedWorld.Characters.Count(c => c.Alive)}, " +
    $"династий {loadedWorld.Dynasties.Count}, семей {loadedWorld.Families.Count}");

// Печатает список строк отчёта в консоль
void PrintLines(IEnumerable<string> lines)
{
    foreach (var line in lines)
    {
        Console.WriteLine(line);
    }
}
