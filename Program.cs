using Aeternum.WorldGen.Characters;
using Aeternum.WorldGen.World;

Console.WriteLine("=================================");
Console.WriteLine("      Aeternum WorldGen");
Console.WriteLine("=================================");

var world = new World();

for (int i = 0; i < 10; i++)
{
    world.Characters.Add(
        CharacterGenerator.Create()
    );
}

Console.WriteLine();
Console.WriteLine("Созданные жители:");
Console.WriteLine();

foreach (var character in world.Characters)
{
    Console.WriteLine(
        $"{character.Name}, возраст {character.Age}"
    );
}