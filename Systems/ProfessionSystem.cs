using Aeternum.WorldGen.Data;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Список профессий, их категории и производство еды — профессия больше не
// декоративная строка, а часть экономики мира (см. EconomySystem).
// Сам список профессий (имя/категория/опасность) — из Data/Professions.json
public static class ProfessionSystem
{
    public static string school = "Школьник"; // Профессия для возраста 7 лет

    // Категория каждой профессии из ProfessionsList
    private static readonly Dictionary<string, ProfessionCategory> Categories =
        ContentData.Professions.ToDictionary(p => p.Name, p => p.Category);

    // Сколько условной еды в год производит один взрослый работник данной категории
    private static readonly Dictionary<ProfessionCategory, double> FoodProductionByCategory = new()
    {
        [ProfessionCategory.FoodProducer] = 4.0,
        [ProfessionCategory.General] = 2.5,
        [ProfessionCategory.Craft] = 2.0,
        [ProfessionCategory.Trade] = 2.0,
        [ProfessionCategory.Knowledge] = 1.0,
        [ProfessionCategory.Military] = 1.0,
    };

    // Сколько золота в год производит один взрослый работник данной категории — только Trade
    // (торговец, купец и т.п.); остальные категории не производят золото
    private static readonly Dictionary<ProfessionCategory, double> GoldProductionByCategory = new()
    {
        [ProfessionCategory.Trade] = 3.0,
    };

    // Какой тип материала производит каждая конкретная ремесленная профессия —
    // привязка один-в-один, а не по грубой категории (см. MaterialType)
    private static readonly Dictionary<string, MaterialType> MaterialTypeByProfession = new()
    {
        ["Кузнец"] = MaterialType.Metal,
        ["Оружейник"] = MaterialType.Metal,
        ["Столяр"] = MaterialType.Wood,
        ["Строитель"] = MaterialType.Wood,
        ["Каменщик"] = MaterialType.Stone,
        ["Ткач"] = MaterialType.Textile,
        ["Портной"] = MaterialType.Textile,
        ["Сапожник"] = MaterialType.Textile,
        ["Кожевник"] = MaterialType.Textile,
        ["Гончар"] = MaterialType.Clay,
        ["Стекольщик"] = MaterialType.Clay,
        ["Ювелир"] = MaterialType.Clay,
        ["Ремесленник"] = MaterialType.Clay,
    };

    // Обратная карта к MaterialTypeByProfession: каким ремёслам учиться в городе,
    // который живёт этим материалом (см. GetRandom)
    private static readonly Dictionary<MaterialType, string[]> ProfessionsByMaterial = MaterialTypeByProfession
        .GroupBy(kv => kv.Value)
        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

    private const double CraftMaterialAmount = 3.0; // Годовое производство материала одним ремесленником своего типа

    // Профессии, которые гарантированно должны быть хотя бы у одного живого жителя
    // поселения — по одному представителю на еду и на каждый тип материала.
    // Без этого поселение могло случайно остаться совсем без кузнеца или земледельца
    private static readonly string[] EssentialProfessions =
    {
        "Фермер", "Кузнец", "Столяр", "Каменщик", "Ткач", "Гончар"
    };

    // Профессии с повышенным риском несчастного случая — используется DeathSystem
    private static readonly HashSet<string> HazardousProfessions = ContentData.Professions
        .Where(p => p.Hazardous)
        .Select(p => p.Name)
        .ToHashSet();

    public static readonly string[] ProfessionsList = Categories.Keys.ToArray();

    // Тот же список без опасных ремёсел — для чистой случайности хрупких (см. GetRandom)
    private static readonly string[] NonHazardousProfessionsList = ProfessionsList
        .Where(p => !HazardousProfessions.Contains(p))
        .ToArray();

    // Профессии, сгруппированные по категории — для культурного смещения в GetRandom
    private static readonly Dictionary<ProfessionCategory, string[]> ProfessionsByCategory = Categories
        .GroupBy(kv => kv.Value)
        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

    // Шанс, что профессия будет выбрана из предпочитаемой культурой категории, а не из всего списка
    private const double CulturePreferenceChance = 0.5;

    // Мастерство: сколько прибавляет к делу каждый год, прожитый в ремесле,
    // и где эта прибавка упирается в потолок. Стаж считается от того года,
    // когда персонаж взялся за нынешнее дело (см. Character.ProfessionYear) —
    // сменивший ремесло начинает с нуля
    private const double MasteryPerYear = 0.02;
    private const double MaxMasteryBonus = 0.5;

    // Шанс, что персонаж унаследует профессию родителя своего пола вместо случайного выбора —
    // семейное ремесло/дело передаётся из поколения в поколение, но не всегда (см. LifeSystem)
    private const double InheritanceChance = 0.4;

    // Шанс на профессию категории Knowledge за одну школу в поселении (см. SchoolSystem)
    private const double SchoolBonusPerSchool = 0.1;
    private const double MaxSchoolBonus = 0.4;

    // Шанс уйти в главное ремесло города за каждую его мастерскую (см. WorkshopSystem).
    // Потолок обязателен: мастерские строятся от числа ремесленников, а ремесленники
    // берутся от мастерских — без предела эта петля схлопнула бы поселение в одно ремесло
    private const double WorkshopBonusPerWorkshop = 0.15;
    private const double MaxWorkshopBonus = 0.4;

    // Насколько сильно щедрая земля тянет в земледелие: на самой плодородной
    // почве (см. ClimateSystem) это примерно каждый третий, на скудной — никого
    private const double FertilityPull = 1.0;

    private const double BraveMilitaryPull = 0.3; // Смелых (см. Trait.Brave) тянет к ратному делу

    // Случайная профессия — по цепочке приоритетов, от самого сильного довода
    // к самому слабому: нехватка обязательной профессии в поселении
    // (см. EssentialProfessions) перевешивает всё; затем семейное дело
    // (профессия родителя); затем школы поселения (категория Knowledge);
    // затем специализация самого места — его главное ремесло (мастерские),
    // плодородие его земли и нрав самого человека (Trait.Brave — к ратному
    // делу); затем культурный уклад (см. Culture.PreferredCategory); в
    // остатке — чистая случайность, но не совсем слепая: слабое здоровье
    // (Trait.Frail) не выбирает опасное ремесло, если ничто другое не потянуло
    // туда сильнее (см. IsHazardous)
    public static string GetRandom(Culture? culture = null, Settlement? settlement = null, string? inheritedProfession = null, HashSet<Trait>? traits = null)
    {
        if (settlement != null)
        {
            var missing = GetMissingEssentialProfessions(settlement);

            if (missing.Count > 0)
            {
                return missing[Rng.Next(missing.Count)];
            }
        }

        if (inheritedProfession != null &&
            Categories.ContainsKey(inheritedProfession) &&
            Rng.NextDouble() < InheritanceChance)
        {
            return inheritedProfession;
        }

        if (settlement is { Schools: > 0 } &&
            Rng.NextDouble() < Math.Min(MaxSchoolBonus, settlement.Schools * SchoolBonusPerSchool) &&
            ProfessionsByCategory.TryGetValue(ProfessionCategory.Knowledge, out var knowledgeProfessions))
        {
            return knowledgeProfessions[Rng.Next(knowledgeProfessions.Length)];
        }

        // Специализация места идёт раньше культурного уклада: чем город живёт
        // сегодня, тому и учат детей — иначе назначенная при рождении мира
        // культура определяла бы занятия жителей вечно, и поселения так и
        // остались бы неотличимы друг от друга
        if (settlement != null && TryPickLocalCraft(settlement, out var localCraft))
        {
            return localCraft;
        }

        if (settlement != null && TryPickFarming(settlement, out var farming))
        {
            return farming;
        }

        if (TryPickByTemperament(traits, out var temperament))
        {
            return temperament;
        }

        if (culture != null &&
            Rng.NextDouble() < CulturePreferenceChance &&
            ProfessionsByCategory.TryGetValue(culture.PreferredCategory, out var preferred))
        {
            return preferred[Rng.Next(preferred.Length)];
        }

        // Чистая случайность — но не совсем слепая: хрупкому не подвернётся
        // опасное ремесло, если до сих пор ничто другое не потянуло его именно туда
        var pool = traits != null && traits.Contains(Trait.Frail) ? NonHazardousProfessionsList : ProfessionsList;

        return pool[Rng.Next(pool.Length)];
    }

    // Смелых тянет в военное дело — тот же принцип, что у TryPickFarming, только
    // не от места, а от собственного нрава, и потому не привязан к поселению
    private static bool TryPickByTemperament(HashSet<Trait>? traits, out string profession)
    {
        profession = "";

        if (traits == null || !traits.Contains(Trait.Brave) ||
            Rng.NextDouble() >= BraveMilitaryPull ||
            !ProfessionsByCategory.TryGetValue(ProfessionCategory.Military, out var military))
        {
            return false;
        }

        profession = military[Rng.Next(military.Length)];

        return true;
    }

    // Город, где стоят мастерские, растит себе смену по тому же ремеслу.
    // Считается по самому развитому ремеслу — у города одно главное дело,
    // а не поровну все сразу
    private static bool TryPickLocalCraft(Settlement settlement, out string profession)
    {
        profession = "";

        var main = settlement.Workshops
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        if (main.Count == 0)
        {
            return false;
        }

        var (type, count) = main[0];

        if (Rng.NextDouble() >= Math.Min(MaxWorkshopBonus, count * WorkshopBonusPerWorkshop) ||
            !ProfessionsByMaterial.TryGetValue(type, out var crafts))
        {
            return false;
        }

        profession = crafts[Rng.Next(crafts.Length)];

        return true;
    }

    // На щедрой земле выгоднее пахать, чем ремесленничать: тяга тем сильнее,
    // чем плодороднее место (см. ClimateSystem). Скудная почва не отталкивает
    // от земли насильно — она просто ничего не добавляет
    private static bool TryPickFarming(Settlement settlement, out string profession)
    {
        profession = "";

        var pull = (ClimateSystem.GetFertility(settlement) - 1.0) * FertilityPull;

        if (pull <= 0 ||
            Rng.NextDouble() >= pull ||
            !ProfessionsByCategory.TryGetValue(ProfessionCategory.FoodProducer, out var farmers))
        {
            return false;
        }

        profession = farmers[Rng.Next(farmers.Length)];

        return true;
    }

    // Любая профессия названной категории — для тех, кто выбирает занятие
    // не сам, а по нужде поселения (см. CareerSystem)
    public static string? PickFromCategory(ProfessionCategory category)
    {
        return ProfessionsByCategory.TryGetValue(category, out var professions)
            ? professions[Rng.Next(professions.Length)]
            : null;
    }

    // Главное ремесло города, если оно у него есть
    public static string? PickLocalCraft(Settlement settlement)
    {
        var main = settlement.Workshops
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        return main.Count > 0 && ProfessionsByMaterial.TryGetValue(main[0].Key, out var crafts)
            ? crafts[Rng.Next(crafts.Length)]
            : null;
    }

    // Первая из обязательных профессий, которой в поселении не занят никто
    public static string? GetMissingEssential(Settlement settlement)
    {
        var missing = GetMissingEssentialProfessions(settlement);

        return missing.Count > 0 ? missing[Rng.Next(missing.Count)] : null;
    }

    // Обязательные профессии, которых сейчас нет ни у одного живого жителя поселения
    private static List<string> GetMissingEssentialProfessions(Settlement settlement)
    {
        var present = settlement.Members
            .Where(m => m.Alive)
            .Select(m => m.Profession)
            .ToHashSet();

        return EssentialProfessions.Where(p => !present.Contains(p)).ToList();
    }

    public static ProfessionCategory GetCategory(string? profession)
    {
        if (profession != null && Categories.TryGetValue(profession, out var category))
        {
            return category;
        }

        return ProfessionCategory.General;
    }

    // Годовое производство еды одним взрослым работником этой профессии
    public static double GetFoodProduction(string? profession)
    {
        return FoodProductionByCategory[GetCategory(profession)];
    }

    // Годовое производство золота одним взрослым работником этой профессии
    public static double GetGoldProduction(string? profession)
    {
        return GoldProductionByCategory.GetValueOrDefault(GetCategory(profession));
    }

    // Годовое производство материалов одним взрослым работником этой профессии:
    // тип и количество. Сырьё даёт только конкретное ремесло — у остальных
    // профессий типа нет вовсе (null), и вклад их в мир идёт едой и золотом
    public static (MaterialType? Type, double Amount) GetMaterialProduction(string? profession)
    {
        if (profession != null && MaterialTypeByProfession.TryGetValue(profession, out var type))
        {
            return (type, CraftMaterialAmount);
        }

        return (null, 0.0);
    }

    // Во сколько раз опытный работник делает своё дело лучше новичка. Умение
    // копится годами и упирается в потолок — дальше растёт только слабость тела
    // (см. EconomySystem.GetProductivity), поэтому мастер на склоне лет работает
    // медленнее себя же в зрелости, но заметно лучше юнца
    public static double GetMastery(Character character, World world)
    {
        if (character.Profession == null)
        {
            return 1.0;
        }

        var yearsInTrade = world.CurrentYear - character.ProfessionYear;

        if (yearsInTrade <= 0)
        {
            return 1.0; // Только взялся за дело — ещё смотрит, как делают другие
        }

        return 1 + Math.Min(MaxMasteryBonus, yearsInTrade * MasteryPerYear);
    }

    public static bool IsHazardous(string? profession)
    {
        return profession != null && HazardousProfessions.Contains(profession);
    }
}
