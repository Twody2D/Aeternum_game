using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Духовенство. "Пастырь" был обычной строкой в списке Knowledge-профессий —
// человек с этим ремеслом кормил поселение и учился наравне с лекарем или
// астрономом, но ни на что не влиял сверх этого. Здесь у сана появляется дело:
// паства, которую держит вместе не сам факт общей веры (см. Religion), а
// то, есть ли у неё пастырь.
//
// Раскол (см. SchismSystem) — разрыв не только с короной, но и с прежней верой.
// Сильное духовенство держит паству крепче — тот же приём, что у стен
// (WallSystem.GetWallFactor) и больниц (HospitalSystem.GetHospitalFactor):
// множитель к шансу, а не запрет. Сан — не только ремесло, но и негласный
// титул: самый умелый пастырь поселения — его духовный глава, вычисляемый,
// а не назначенный (см. GetSpiritualHead), тем же приёмом, что и светские
// должности при дворе (см. CourtSystem), только без стола и назначения —
// сан у духовенства свой, мирская иерархия его не выбирает
public static class ClergySystem
{
    private const double MaxCohesionBonus = 0.5; // Максимум -50% к шансу раскола
    private const double CohesionBonusPerInfluence = 0.15;

    // Понижающий множитель для шанса раскола (см. SchismSystem) — 1.0 без
    // духовенства, ниже — чем опытнее и многочисленнее пастыри поселения
    public static double GetCohesionFactor(Settlement settlement, World world)
    {
        var influence = GetInfluence(settlement, world);
        var bonus = Math.Min(MaxCohesionBonus, influence * CohesionBonusPerInfluence);

        return 1 - bonus;
    }

    // Не число голов, а умение — тот же принцип, что у гарнизона (см. ArmySystem.GetGarrisonStrength):
    // опытный пастырь держит паству крепче новопосвящённого
    private static double GetInfluence(Settlement settlement, World world)
    {
        return settlement.Members
            .Where(m => m.Alive && ProfessionSystem.IsClergy(m.Profession))
            .Sum(m => ProfessionSystem.GetMastery(m, world));
    }

    // Духовный глава поселения — самый умелый из его пастырей, если такой есть.
    // Не должность, а сан: никто его не назначает, он вычисляется заново каждый
    // раз, как и любой другой факт о мире
    public static Character? GetSpiritualHead(Settlement settlement, World world)
    {
        return settlement.Members
            .Where(m => m.Alive && ProfessionSystem.IsClergy(m.Profession))
            .OrderByDescending(m => ProfessionSystem.GetMastery(m, world))
            .ThenBy(m => m.Id)
            .FirstOrDefault();
    }
}
