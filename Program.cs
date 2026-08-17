using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Settings;
using Aeternum.WorldGen.Systems;



Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();
world.AliveCount = ProjectSettings.StartingPopulation; // Инициализируем счетчик живых персонажей

for (int i = 0; i < ProjectSettings.StartingPopulation; i++)    // Создаем указанное количество случайных персонажей
{
    world.Characters.Add(
        CharacterGenerator.Create() // Добавляем нового персонажа в список Characters
    );
}

PrintLines(StatisticsSystem.BuildInitialPopulationReport(world));

var engine = new SimulationEngine();

engine.Run(world, ProjectSettings.SimulationYears, w =>   // Консоль сама решает, как показать прогресс года
{
    Console.WriteLine($"===== Год {w.CurrentYear} =====");

    foreach (var yearEvent in EventSystem.GetYearEvents(w))
    {
        Console.WriteLine(yearEvent.Description);
    }
});

PrintLines(StatisticsSystem.BuildFinalReport(world));

void PrintLines(IEnumerable<string> lines)
{
    foreach (var line in lines)
    {
        Console.WriteLine(line);
    }
}
