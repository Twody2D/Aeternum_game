using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Simulation;

namespace Aeternum.WorldGen.Core;

public class SimulationEngine
{
    private readonly Random _random = new(); // Генератор случайных чисел для симуляции

    public void Run(World world, int years)         // Метод для запуска симуляции на определенное количество лет
    {
        // for (int year = 1; year <= years; year++)   // Цикл по годам симуляции
        // {
        //     Console.WriteLine();
        //     Console.WriteLine($"Год {year}");
        //     Console.WriteLine("----------------");

        //     world.CurrentYear++;                        // Увеличиваем текущий год на 1


        //     List<Character> newborns = new();
        //     foreach (var character in world.Characters) // Цикл по всем персонажам в мире
        //     {
        //         if (!character.Alive)
        //         {
        //             continue;
        //         }
        //         character.Age++;                
        //                               // Увеличиваем возраст персонажа на 1
                
        //         LifeSystem.UpdateLifeStage(character); 
        //         DeathSystem.Process(world); // Проверяем, жив ли персонаж, и обрабатываем его смерть, если необходимо    
        //         if (!character.Alive)
        //         {
        //             continue;
        //         }
        //         LifeSystem.AssignProfession(character);                // Назначаем профессию персонажу на основе его возраста
               
        //         Console.WriteLine(
        //             $"{character.Name}, возраст {character.Age}, {character.Profession}"); // Выводим информацию о персонаже
        //     }
        //     BirthSystem.ProcessBirths(newborns,world); // Обрабатываем рождение новых персонажей в мире

        //     world.Characters.AddRange(newborns); // Добавляем новорожденных персонажей в список Characters
                            

                            
                
        // }
        for(int i = 0; i < years; i++)
        {
            YearProcessor.Process(world);
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