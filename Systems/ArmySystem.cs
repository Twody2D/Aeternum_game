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

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

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

    // Живые воины государства
    public static List<Character> GetSoldiers(Kingdom kingdom)
    {
        return kingdom.Settlements
            .SelectMany(s => s.Members)
            .Where(m => m.Alive && ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military)
            .ToList();
    }

    // Сила войска: не число голов, а сумма их умения, усиленная воеводой
    public static double GetStrength(Kingdom kingdom, World world)
    {
        var skill = GetSoldiers(kingdom).Sum(s => ProfessionSystem.GetMastery(s, world));

        return skill * CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Marshal, world);
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
