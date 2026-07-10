using Aeternum.WorldGen.GameWorld;
using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.Simulation;

public class SimulationEngine
{
    private readonly Random _random = new(); // Генератор случайных чисел для симуляции

    public void Run(World world, int years)         // Метод для запуска симуляции на определенное количество лет
    {
        int aliveCount = ProjectSetting.StartingPopulation; // Счетчик живых персонажей
        for (int year = 1; year <= years; year++)   // Цикл по годам симуляции
        {
            Console.WriteLine();
            Console.WriteLine($"Год {year}");
            Console.WriteLine("----------------");

            world.CurrentYear++;                        // Увеличиваем текущий год на 1



            foreach (var character in world.Characters) // Цикл по всем персонажам в мире
            {
                if (!character.Alive)                        // Если персонаж мертв
                {
                    continue;                            // Переходим к следующему персонажу, если текущий мертв
                }
                character.Age++;                
                                      // Увеличиваем возраст персонажа на 1


            if (character.Age == 7 && character.Profession == null)
                {
                    character.Profession = ProfessionGenerator.school; // Назначаем профессию "Школьник" для персонажей, достигших возраста 7 лет
                }
             if (character.Age == 16 && character.Profession == ProfessionGenerator.school)
                {
                    character.Profession = ProfessionGenerator.GetRandom(); // Назначаем случайную профессию для персонажей в возрасте от 16
                }

            if (character.Age > 60)                      // Если возраст персонажа больше 80 лет
                {
                    int deathChance = character.Age - 60; // Шанс смерти увеличивается с возрастом
                    if (_random.Next(100) < deathChance)  // Генерируем случайное число и сравниваем с шансом смерти
                        {
                        character.Alive = false;            // Персонаж умирает
                                Console.WriteLine(
                                    $"{character.Name} умер в возрасте {character.Age}"); // Выводим информацию о смерти персонажа
                            world.TotalDeaths++;                 // Увеличиваем счетчик смертей в мире  
                            aliveCount--;                           // Уменьшаем счетчик живых персонажей
                        continue;
                        }
                }
                Console.WriteLine(
                    $"{character.Name}, возраст {character.Age}, {character.Profession}"); // Выводим информацию о персонаже
                            }
            if (_random.Next(100) < 20)
            {
            var newborn = CharacterGenerator.CreateNewborn();
            world.Characters.Add(newborn); // Добавляем новорожденного персонажа в список Characters
            world.TotalBirths++;


                Console.WriteLine($"Родился {newborn.Name}");
                aliveCount++;
            }
        }
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("Статистика мира");
        Console.WriteLine("=================================");
        Console.WriteLine($"Возраст мира: {world.CurrentYear} лет");
        Console.WriteLine($"Всего рождений: {world.TotalBirths}");
        Console.WriteLine($"Всего смертей: {world.TotalDeaths}");
        Console.WriteLine($"Живых персонажей: {aliveCount}");
    }
}