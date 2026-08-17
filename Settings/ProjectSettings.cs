namespace Aeternum.WorldGen.Settings;

// Константы запуска приложения (в отличие от WorldSettings — не относятся к самому миру)
public static class ProjectSettings
{
    // Количество жителей, с которых начинается симуляция
    public static int StartingPopulation { get; } = 30; 

    // Количество лет, на которые запускается симуляция
    public static int SimulationYears { get; } = 100; 

}