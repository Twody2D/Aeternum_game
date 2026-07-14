using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;
using Aeternum.WorldGen.Simulation;

namespace Aeternum.WorldGen.Core;

public class SimulationEngine
{
    private readonly Random _random = new(); // Генератор случайных чисел для симуляции

    public void Run(World world, int years)         // Метод для запуска симуляции на определенное количество лет
    {
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