using Aeternum.WorldGen.GameWorld;

namespace Aeternum.WorldGen.Simulation;

public class SimulationEngine
{
    public void Run(World world, int years)         // Метод для запуска симуляции на определенное количество лет
    {
        for (int year = 1; year <= years; year++)   // Цикл по годам симуляции
        {
            Console.WriteLine();
            Console.WriteLine($"Год {year}");
            Console.WriteLine("----------------");

            world.CurrentYear++;                        // Увеличиваем текущий год на 1

            foreach (var character in world.Characters) // Цикл по всем персонажам в мире
            {
                character.Age++;                        // Увеличиваем возраст персонажа на 1

                Console.WriteLine(
                    $"{character.Name}, возраст {character.Age}" // Выводим информацию о персонаже
                );
            }
        }
    }
}