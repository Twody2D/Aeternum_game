using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Generators;


public static class WorldGenerator
{
    public static World Create()
    {
        var world = new World();


        // Создаём первых жителей
        for(int i = 0; i < 10; i++)
        {
            var character = CharacterGenerator.Create();

            world.Characters.Add(character);
        }


        CreateStartingFamilies(world);


        world.AliveCount = world.Characters.Count;


        return world;
    }



    private static void CreateStartingFamilies(World world)
    {
        var males = world.Characters
            .Where(c => c.Gender == Gender.Male)
            .ToList();


        var females = world.Characters
            .Where(c => c.Gender == Gender.Female)
            .ToList();


        int familyCount = Math.Min(
            males.Count,
            females.Count
        );


        for(int i = 0; i < familyCount; i++)
        {
            FamilySystem.CreateFamily(
                females[i],
                males[i],
                world
            );
        }
    }
}