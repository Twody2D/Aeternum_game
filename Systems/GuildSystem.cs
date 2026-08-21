using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Гильдии. Мастерские (см. WorkshopSystem) уже отвечают на вопрос "сколько
// поселение производит", но ничего не говорят о том, чьих рук это дело:
// кузница у новичка и кузница у мастеров стоит на рынке (см. MarketSystem)
// одинаково, хотя работа мастеров ценится выше. Гильдия — не постройка и
// не постоянный список, а сам собой сложившийся круг ремесленников одного
// материала в одном поселении: чем они опытнее (см. ProfessionSystem.GetMastery),
// тем дороже уходит их товар за пределы поселения.
//
// Глава цеха (см. GetGuildmaster) — не назначенная должность, а сан того же
// рода, что и духовный глава (см. ClergySystem): просто самый умелый из ныне
// живущих мастеров ремесла, вычисляемый заново каждый раз
public static class GuildSystem
{
    private const double MaxQualityPremium = 0.6; // Максимум +60% к цене сырья за счёт мастерства цеха
    private const double PremiumPerMastery = 0.1;

    // Множитель к цене сырья на внешнем рынке (см. MarketSystem.SellSurplus) —
    // 1.0 без цеха, выше — чем опытнее и многочисленнее его мастера
    public static double GetQualityPremium(Settlement settlement, MaterialType type, World world)
    {
        var guildStrength = GetGuildStrength(settlement, type, world);

        return 1 + Math.Min(MaxQualityPremium, guildStrength * PremiumPerMastery);
    }

    // Глава цеха — самый умелый из живущих в поселении мастеров этого материала,
    // если такой есть
    public static Character? GetGuildmaster(Settlement settlement, MaterialType type, World world)
    {
        return GetGuildMembers(settlement, type)
            .OrderByDescending(m => ProfessionSystem.GetMastery(m, world))
            .ThenBy(m => m.Id)
            .FirstOrDefault();
    }

    private static double GetGuildStrength(Settlement settlement, MaterialType type, World world)
    {
        return GetGuildMembers(settlement, type).Sum(m => ProfessionSystem.GetMastery(m, world));
    }

    private static IEnumerable<Character> GetGuildMembers(Settlement settlement, MaterialType type)
    {
        return settlement.Members.Where(m => m.Alive && ProfessionSystem.GetMaterialProduction(m.Profession).Type == type);
    }
}
