using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Войско. До сих пор армии как таковой не существовало: там, где нужна была
// военная сила, каждая система заново пересчитывала живых жителей с профессией
// Military — оборона поселения по головам, поход на мятежников по головам,
// и ничего это государству не стоило.
//
// Здесь войско становится единой величиной с двумя свойствами, которых у
// набора голов быть не может. Во-первых, сила: бывалый воин стоит нескольких
// новобранцев (см. ProfessionSystem.GetMastery), а под рукой воеводы всё
// войско воюет лучше (см. CourtSystem). Во-вторых, цена: войско надо кормить
// из казны, и корона, которой нечем платить, теряет его — воины расходятся
// по мирным ремёслам (см. CareerSystem, там же уходят от голода).
//
// Отсюда сам собой выходит выбор, которого в мире не было: большая армия
// разоряет казну, пустая казна распускает армию, а бедная корона беззащитна
// именно потому, что бедна
public static class ArmySystem
{
    private const double UpkeepPerSoldier = 4.0; // Сколько еды в год стоит короне один воин
    private const double DesertionShare = 0.25; // Какая часть войска расходится за год, когда платить нечем

    // Наёмники — не постоянное войско, а решение на один год войны: нанимает
    // только тот, кто прямо сейчас держит осаду (см. WarSystem.Settlement.SiegeYears)
    // и одновременно небогат своими воинами при богатой казне. Без привязки к
    // войне богатая корона нанимала бы наёмников каждый мирный год подряд —
    // не "усиление войска на трудный год", а постоянная вторая армия
    // задаром. MinTreasuryToHire — порог "большой казны" (ниже него не до
    // наёмников, надо беречь золото на еду); тратится не вся казна сверх
    // порога, а её доля — наём не разоряет корону подчистую
    private const int MinSoldiersPerSettlement = 2; // Ниже этого своих воинов на поселение — уже нехватка
    private const double MinTreasuryToHireMercenaries = 200;
    private const double MercenaryBudgetShare = 0.3;
    private const double MercenaryStrengthPerGold = 0.02;

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            HireMercenaries(kingdom, world);

            var soldiers = GetSoldiers(kingdom);

            if (soldiers.Count == 0)
            {
                continue;
            }

            var upkeep = soldiers.Count * UpkeepPerSoldier;

            if (kingdom.FoodTreasury >= upkeep)
            {
                kingdom.FoodTreasury -= upkeep;
                continue;
            }

            // Платить нечем: часть войска расходится по мирным ремёслам.
            // Казна отдаёт всё, что есть, — этим и держится остаток
            kingdom.FoodTreasury = 0;

            var leaving = (int)Math.Ceiling(soldiers.Count * DesertionShare);

            var deserters = soldiers
                .OrderBy(s => ProfessionSystem.GetMastery(s, world)) // Первыми уходят те, кому меньше терять
                .ThenBy(s => s.Id)
                .Take(leaving)
                .ToList();

            foreach (var deserter in deserters)
            {
                deserter.Profession = ProfessionSystem.PickFromCategory(ProfessionCategory.FoodProducer) ?? deserter.Profession;
                deserter.ProfessionYear = world.CurrentYear; // Прежнее умение с собой не переносится
            }

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Desertion,
                Description = $"{kingdom.Name}: казне нечем платить войску — разошлось {deserters.Count} человек",
                Kingdoms = [kingdom]
            });
        }
    }

    // Наёмная сила на этот год: платит только тот, кому есть чем платить и
    // некому больше воевать своими руками. Решение не хранится дольше года —
    // не наняли сейчас, значит нечем, и MercenaryStrength обнуляется
    private static void HireMercenaries(Kingdom kingdom, World world)
    {
        var isAtWar = kingdom.Settlements.Any(s => s.SiegeYears > 0);
        var ownSoldiers = GetSoldiers(kingdom).Count;
        var needsMercenaries = isAtWar && ownSoldiers < kingdom.Settlements.Count * MinSoldiersPerSettlement;

        if (!needsMercenaries || kingdom.GoldTreasury <= MinTreasuryToHireMercenaries)
        {
            kingdom.MercenaryStrength = 0;
            return;
        }

        var budget = (kingdom.GoldTreasury - MinTreasuryToHireMercenaries) * MercenaryBudgetShare;

        kingdom.GoldTreasury -= budget;
        kingdom.MercenaryStrength = budget * MercenaryStrengthPerGold;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Mercenaries,
            Description = $"{kingdom.Name}: нанято наёмников за {budget:F0} золота (сила +{kingdom.MercenaryStrength:F1})",
            Kingdoms = [kingdom]
        });
    }

    // Живые воины государства
    public static List<Character> GetSoldiers(Kingdom kingdom)
    {
        return kingdom.Settlements
            .SelectMany(s => s.Members)
            .Where(m => m.Alive && ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military)
            .ToList();
    }

    // Сила войска: не число голов, а сумма их умения плюс купленная на этот год
    // наёмная сила (см. HireMercenaries), всё вместе усиленное воеводой
    public static double GetStrength(Kingdom kingdom, World world)
    {
        var skill = GetSoldiers(kingdom).Sum(s => ProfessionSystem.GetMastery(s, world));

        return (skill + kingdom.MercenaryStrength) * CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Marshal, world);
    }

    // Сила гарнизона одного поселения — тем же счётом, что и сила государства,
    // только без воеводы: он при войске, а не в каждом городе разом
    public static double GetGarrisonStrength(Settlement settlement, World world)
    {
        return settlement.Members
            .Where(m => m.Alive && ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military)
            .Sum(m => ProfessionSystem.GetMastery(m, world));
    }
}
