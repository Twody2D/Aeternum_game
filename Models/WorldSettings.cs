namespace Aeternum.WorldGen.Models;


// Настраиваемые параметры мира: возрастные пороги и рождаемость
public class WorldSettings
{
    // Возраст

    public int AdultAge { get; set; } = 18; // С какого возраста персонаж считается взрослым (брак, статистика)

    public int ElderAge { get; set; } = 65; // С какого возраста персонаж считается пожилым (статистика)



    // Рождение

    // Базовый коэффициент рождаемости (10% шанс на рождение ребёнка в год у одной семьи)
    public double BaseBirthRate { get; set; } = 0.10;

    // Порог низкого населения
    public int LowPopulationThreshold { get; set; } = 100;

    // 30% шанс на рождение ребёнка в год при низком населении
    public double LowPopulationBirthRate { get; set; } = 0.30;

    // Порог критически низкого населения
    public int CriticalPopulationThreshold { get; set; } = 10;

    // 50% шанс на рождение ребёнка в год при критически низком населении
    public double CriticalBirthRate { get; set; } = 0.50;



    // Смерть

    public int MaximumAge { get; set; } = 100; // Предельный возраст — по достижении смерть гарантирована
}
