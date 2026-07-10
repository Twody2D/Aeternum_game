using Aeternum.WorldGen.Characters;
using Aeternum.WorldGen.GameWorld;
using Aeternum.WorldGen.Simulation;

Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();

for (int i = 0; i < 10; i++)    // Создаем 10 случайных персонажей
{
    world.Characters.Add(
        CharacterGenerator.Create() // Добавляем нового персонажа в список Characters
    );
}

Console.WriteLine();
Console.WriteLine("Созданные жители:");
Console.WriteLine();

var engine = new SimulationEngine();
engine.Run(world, 5); // Запускаем симуляцию на 5 лет



foreach (var character in world.Characters) // Выводим информацию о каждом персонаже
{
    Console.WriteLine(
        $"{character.Name}, возраст {character.Age}"
    );
}