namespace Aeternum.WorldGen.Models;


public class WorldSettings
{
    // Возраст

    public int AdultAge { get; set; } = 18;

    public int ElderAge { get; set; } = 65;



    // Рождение

    // Базовый коэффициент рождаемости (25% шанс на рождение ребенка в год)
    public double BaseBirthRate { get; set; } = 0.10;

    // Порог низкого населения
    public int LowPopulationThreshold { get; set; } = 100;

    // 40% шанс на рождение ребенка в год при низком населении
    public double LowPopulationBirthRate { get; set; } = 0.30; 

    // Порог критически низкого населения
    public int CriticalPopulationThreshold { get; set; } = 10; 

    // 70% шанс на рождение ребенка в год при критически низком населении
    public double CriticalBirthRate { get; set; } = 0.50; 



    // Смерть

    public int MaximumAge { get; set; } = 100;
}