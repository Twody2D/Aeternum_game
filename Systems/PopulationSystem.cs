using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Settings;

namespace Aeternum.WorldGen.Systems;


public static class PopulationSystem
{
    // Все живые жители
    public static int GetAlivePopulation(World world)
    {
        return world.Characters.Count(c => c.Alive);
    }


    // Женщины, которые могут иметь детей
    public static List<Character> GetAdultFemales(World world)
    {
        return world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Female &&
                c.LifeStage == LifeStage.Adult &&
                c.Age >= 18 &&
                c.Age <= 45)
            .ToList();
    }

    // Мужчины, которые могут быть отцами
    public static List<Character> GetPotentialFathers(World world)
    {
        return world.Characters
            .Where(c =>
                c.Alive &&
                c.Gender == Gender.Male &&
                c.LifeStage == LifeStage.Adult &&
                c.Age >= 18)
            .ToList();
    }


    // Коэффициент рождаемости
    public static double GetBirthRate(World world)
    {
        int population = GetAlivePopulation(world);


        double rate = world.Settings.BaseBirthRate;


        // если население падает
        if(population < world.Settings.LowPopulationThreshold)
        {
            rate = world.Settings.LowPopulationBirthRate;
        }


        if(population < world.Settings.CriticalPopulationThreshold)
        {
            rate = world.Settings.CriticalBirthRate;
        }


        return rate;
    }
}