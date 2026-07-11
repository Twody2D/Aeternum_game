using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;



Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();
world.AliveCount = ProjectSetting.StartingPopulation; // Инициализируем счетчик живых персонажей

for (int i = 0; i < ProjectSetting.StartingPopulation; i++)    // Создаем указанное количество случайных персонажей
{
    world.Characters.Add(
        CharacterGenerator.Create() // Добавляем нового персонажа в список Characters
    );
}

Console.WriteLine();
Console.WriteLine("Созданные жители:");
Console.WriteLine();

var engine = new SimulationEngine();
engine.Run(world, ProjectSetting.SimulationYears); // Запускаем симуляцию на указанное количество лет


// foreach (var character in world.Characters) // Выводим информацию о каждом персонаже
// {
//     Console.WriteLine(
//         $"{character.Name}, возраст {character.Age}");
// }