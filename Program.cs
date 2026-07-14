using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Settings;



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

Console.WriteLine();
Console.WriteLine("Созданные жители:");
Console.WriteLine();

var engine = new SimulationEngine();
engine.Run(world, ProjectSettings.SimulationYears); // Запускаем симуляцию на указанное количество лет


foreach(var e in world.Events)
{
    Console.WriteLine(
        $"{e.Year}: {e.Description}"
    );
}