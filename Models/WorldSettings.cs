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

    public double InfantMortalityRate { get; set; } = 0.05; // Шанс смерти младенца (0-2 года) от болезни за год

    public double AccidentRate { get; set; } = 0.01; // Базовый шанс несчастного случая в год для любого персонажа

    public double HazardousProfessionMultiplier { get; set; } = 3.0; // Во сколько раз растёт риск несчастного случая у опасных профессий



    // Экономика (еда)

    public double FoodConsumptionPerCapita { get; set; } = 1.0; // Сколько еды в год потребляет один живой персонаж

    public double StarvationSeverity { get; set; } = 0.15; // Множитель шанса смерти от голода при сильном дефиците еды



    // Развод

    public int ChildlessDivorceThresholdYears { get; set; } = 10; // Через сколько лет бездетного брака появляется риск развода

    public double DivorceChance { get; set; } = 0.03; // Шанс развода в год для брака, превысившего порог бездетности



    // Миграция

    public double MigrationChance { get; set; } = 0.2; // Шанс в год для одинокого взрослого без детей уехать из голодающего поселения
}
