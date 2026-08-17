using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Смерть от старости — единственная реализованная причина смерти в игре на данный момент
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

            if (character.Age >= world.Settings.MaximumAge) // Предельный возраст — смерть гарантирована
            {
                Kill(character, world);
                continue;
            }

            if (character.LifeStage == LifeStage.Elder)  // Возраст 60+ — растущий с возрастом шанс смерти
            {
                int deathChance = character.Age - 60; // Шанс смерти увеличивается с возрастом
                if (_random.Next(100) < deathChance)  // Генерируем случайное число и сравниваем с шансом смерти
                {
                    Kill(character, world); // Если персонаж умирает, вызываем метод Kill
                }
            }
        }
    }
     // Помечает персонажа мёртвым и логирует событие смерти
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
            Description = $"{character.Name} {character.LastName} умер в возрасте {character.Age}"
        });
    }
}
