using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;


public static class StatisticsSystem
{
    public static void PrintInitialPopulation(World world)
    {
        Console.WriteLine();
        Console.WriteLine("===== Начальное население =====");

        foreach (var character in world.Characters)
    {
        Console.WriteLine(
            $"{character.Name}, {character.Age} лет, {character.Profession}");
    }

    Console.WriteLine();
    }
    public static void Print(World world)
    {
        Console.WriteLine();
        Console.WriteLine("===== Итоги симуляции =====");


        Console.WriteLine(
            $"Возраст мира: {world.CurrentYear} лет");


        Console.WriteLine(
            $"Всего жителей создано: {world.Characters.Count}");


        Console.WriteLine(
            $"Живых персонажей: {world.Characters.Count(c => c.Alive)}");


        Console.WriteLine(
            $"Всего рождений: {world.TotalBirths}");


        Console.WriteLine(
            $"Всего смертей: {world.TotalDeaths}");


        Console.WriteLine();


        Console.WriteLine("Распределение по возрасту:");

        var ageGroups = world.Characters
            .Where(c => c.Alive)
            .GroupBy(c =>
            {
                if(c.Age < 18)
                    return "Дети";

                if(c.Age < 65)
                    return "Взрослые";

                return "Пожилые";
            });


        foreach(var group in ageGroups)
        {
            Console.WriteLine(
                $"{group.Key}: {group.Count()}");
        }


        Console.WriteLine();
    }
}