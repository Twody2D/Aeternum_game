using Aeternum.WorldGen.Characters;
using Aeternum.WorldGen.GameWorld;
namespace Aeternum.WorldGen.Simulation;

public static class BirthSystem
{
    private static readonly Random _random = new();
    public static void ProcessBirths(List<Character> newborns, World world)
    {
        int adults = world.Characters.Count(c => c.Alive && c.LifeStage == LifeStage.Adult); // Количество взрослых персонажей в мире
        int birthAttempts = Math.Max(1, adults / 20);
        for (int i = 0; i < birthAttempts; i++)
        {
            if (_random.Next(100) < 30)
            {
            var newborn = CharacterGenerator.CreateNewborn();
            newborns.Add(newborn); // Добавляем новорожденного персонажа в список newborns
            
            world.TotalBirths++;
            world.AliveCount++;

                Console.WriteLine($"Родился {newborn.Name}");
                
            }
        }
    }
}