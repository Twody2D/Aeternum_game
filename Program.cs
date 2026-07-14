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
StatisticsSystem.PrintInitialPopulation(world);

// Console.WriteLine();
// Console.WriteLine("Созданные жители:");
// Console.WriteLine();



var engine = new SimulationEngine();
engine.Run(world, ProjectSettings.SimulationYears); // Запускаем симуляцию на указанное количество лет


