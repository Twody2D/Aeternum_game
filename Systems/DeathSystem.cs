using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;
public static class DeathSystem
{
    private static readonly Random _random = new();
    public static void Process(World world)
    {
        foreach (var character in world.Characters)
        {
            if (!character.Alive)                        // Если персонаж мертв
            {
                continue;                            // Переходим к следующему персонажу, если текущий мертв
            }

                if (character.Age > 60)                      // Если возраст персонажа больше 80 лет
                {
                    int deathChance = character.Age - 60; // Шанс смерти увеличивается с возрастом
                    if (_random.Next(100) < deathChance)  // Генерируем случайное число и сравниваем с шансом смерти
                        {
                        Kill(character, world); // Если персонаж умирает, вызываем метод Kill
                        }
                }
        }
    }
     private static void Kill(Character character, World world)
    {
        character.Alive = false;
        character.DeathReason = DeathReason.OldAge;

        world.TotalDeaths++;
        world.AliveCount--;
        
        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Death,
            Description = $"{character.Name} умер в возрасте {character.Age}"
        });
    }
}