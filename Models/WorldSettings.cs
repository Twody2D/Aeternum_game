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

    public double AccidentRate { get; set; } = 0.005; // Базовый шанс несчастного случая в год (для взрослых и стариков)

    public double HazardousProfessionMultiplier { get; set; } = 3.0; // Во сколько раз растёт риск несчастного случая у опасных профессий



    // Экономика (еда)

    public double FoodConsumptionPerCapita { get; set; } = 1.0; // Сколько еды в год потребляет один живой персонаж

    public double StarvationSeverity { get; set; } = 0.15; // Множитель шанса смерти от голода при сильном дефиците еды



    // Развод

    public int ChildlessDivorceThresholdYears { get; set; } = 10; // Через сколько лет бездетного брака появляется риск развода

    public double DivorceChance { get; set; } = 0.03; // Шанс развода в год для брака, превысившего порог бездетности



    // Миграция

    public double MigrationChance { get; set; } = 0.2; // Шанс в год для одинокого взрослого без детей уехать из голодающего поселения



    // Катастрофы

    public double DisasterChance { get; set; } = 0.02; // Шанс катастрофы в поселении за год (эпидемия либо неурожай, 50/50)

    public double EpidemicMortalityRate { get; set; } = 0.15; // Доля погибших от эпидемии среди всех живых жителей поселения

    public double CropFailureFoodLossFactor { get; set; } = 1.0; // Неурожай отнимает у поселения столько лет его суммарного потребления из запаса



    // Войны

    public double WarChance { get; set; } = 0.1; // Шанс в год, что спор за поселение между государствами обернётся войной

    public double WarCasualtyRate { get; set; } = 0.1; // Доля погибших среди живых жителей спорного поселения за одну войну



    // Династии

    // Шанс, что ребёнок унаследует родной дом матери вместо дома отца — противовес
    // строго патрилинейной концентрации (см. FamilySystem.AddChildToFamily)
    public double MaternalDynastyChance { get; set; } = 0.1;



    // Колонизация

    // Живого населения в поселении, начиная с которого рассматриваем колонизацию
    public int ColonizationPopulationThreshold { get; set; } = 25;

    // Шанс в год, что переполненное поселение породит новое (см. ColonizationSystem)
    public double ColonizationChance { get; set; } = 0.05;

    // Сколько материалов должно быть у поселения-родителя, чтобы позволить себе колонизацию
    public double ColonizationMaterialCost { get; set; } = 50;



    // Заговоры

    // Шанс в год, что правителя с живым взрослым соперником по династии убьют (см. MurderSystem)
    public double RegicideChance { get; set; } = 0.03;



    // Союзы

    // Шанс в год, что два ещё не союзных государства с единоверными правителями заключат союз (см. AllianceSystem)
    public double AllianceChance { get; set; } = 0.05;



    // Кризис наследования

    // Шанс, что переход трона не к прямому потомку обернётся распрей (см. KingdomSystem.TriggerSuccessionCrisis)
    public double SuccessionCrisisChance { get; set; } = 0.15;

    // Доля погибших среди остальных живых членов династии за одну распрю
    public double CivilWarCasualtyRate { get; set; } = 0.05;



    // Торговля

    // Доля излишка (еды или материалов), которую поселение может отдать за год
    // соседям по государству с дефицитом (см. TradeSystem)
    public double TradeTransferRate { get; set; } = 0.3;



    // Постройки

    // Шанс в год, что одна лишняя (не нужная нынешнему населению) постройка
    // придёт в негодность — см. DecaySystem
    public double BuildingDecayChance { get; set; } = 0.15;



    // Дань

    // Доля излишка (еды или материалов), которую государство забирает в казну за год (см. TributeSystem)
    public double TributeRate { get; set; } = 0.1;

    // Чем больше казна относительно этого числа, тем ниже эффективный шанс кризиса
    // наследования (см. KingdomSystem.TryTriggerSuccessionCrisis) — богатое государство
    // стабильнее, но полностью риск не убирает
    public double TreasuryStabilityDivisor { get; set; } = 500;
}
