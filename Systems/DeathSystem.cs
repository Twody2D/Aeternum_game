using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;
public static class DeathSystem
{
    private static readonly Random _random = new();
    public static void Process(Character character, World world)
    {
                if (!character.Alive)                        // Если персонаж мертв
                {
                    return;                            // Переходим к следующему персонажу, если текущий мертв
                }

                if (character.Age > 60)                      // Если возраст персонажа больше 80 лет
                {
                    int deathChance = character.Age - 60; // Шанс смерти увеличивается с возрастом
                    if (_random.Next(100) < deathChance)  // Генерируем случайное число и сравниваем с шансом смерти
                        {
                        character.Alive = false;            // Персонаж умирает
                        character.DeathReason = DeathReason.OldAge; // Устанавливаем причину смерти
                                Console.WriteLine(
                                    $"{character.Name} умер в возрасте {character.Age}"); // Выводим информацию о смерти персонажа
                            world.TotalDeaths++;                 // Увеличиваем счетчик смертей в мире  
                            world.AliveCount--;                           // Уменьшаем счетчик живых персонажей
                        return;
                        }
                }
    }
}