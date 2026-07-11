using Aeternum.WorldGen.GameWorld;
using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.Simulation;

public class SimulationEngine
{
    private readonly Random _random = new(); // Генератор случайных чисел для симуляции

    public void Run(World world, int years)         // Метод для запуска симуляции на определенное количество лет
    {
       // int aliveCount = ProjectSetting.StartingPopulation; // Счетчик живых персонажей
        for (int year = 1; year <= years; year++)   // Цикл по годам симуляции
        {
            Console.WriteLine();
            Console.WriteLine($"Год {year}");
            Console.WriteLine("----------------");

            world.CurrentYear++;                        // Увеличиваем текущий год на 1



            foreach (var character in world.Characters) // Цикл по всем персонажам в мире
            {
                if (!character.Alive)
                {
                    continue;
                }
                character.Age++;                
                                      // Увеличиваем возраст персонажа на 1
                
                LifeSystem.UpdateLifeStage(character); 
                DeathSystem.Process(character, world); // Проверяем, жив ли персонаж, и обрабатываем его смерть, если необходимо                // Обновляем этап жизни персонажа на основе его возраста
                LifeSystem.AssignProfession(character);                // Назначаем профессию персонажу на основе его возраста
                
                Console.WriteLine(
                    $"{character.Name}, возраст {character.Age}, {character.Profession}"); // Выводим информацию о персонаже
                            }
            if (_random.Next(100) < 90)
            {
            var newborn = CharacterGenerator.CreateNewborn();
            world.Characters.Add(newborn); // Добавляем новорожденного персонажа в список Characters
            world.TotalBirths++;


                Console.WriteLine($"Родился {newborn.Name}");
                world.AliveCount++;
            }
        }
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("Статистика мира");
        Console.WriteLine("=================================");
        Console.WriteLine($"Возраст мира: {world.CurrentYear} лет");
        Console.WriteLine($"Всего рождений: {world.TotalBirths}");
        Console.WriteLine($"Всего смертей: {world.TotalDeaths}");
        Console.WriteLine($"Живых персонажей: {world.AliveCount}");
    }
}