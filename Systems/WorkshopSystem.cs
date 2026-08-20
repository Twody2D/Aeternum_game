using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Третий шаг фазы "эволюция": мастерские усиливают саму экономику, а не
// демографию (в отличие от домов/больниц). Чем больше в поселении живых
// ремесленников одного типа материала, тем быстрее строится мастерская
// этого типа — из того же материала, который она потом усиливает
// (кузница из металла, а не дерева). Замыкает цикл профессия → материал →
// постройка → усиленное производство того же материала
public static class WorkshopSystem
{
    private const int CraftersPerWorkshop = 3; // Нужно минимум 3 ремесленника типа, чтобы претендовать на первую мастерскую
    private const double WorkshopCost = 25;

    private const double MaxProductionBonus = 1.0; // Максимум ×2 производства при достаточном числе мастерских
    private const double BonusPerWorkshop = 0.2;

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var craftersByType = settlement.Members
                .Where(m => m.Alive)
                .Select(m => ProfessionSystem.GetMaterialProduction(m.Profession).Type)
                .Where(t => t != null)
                .Select(t => t!.Value)
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());

            // Идём и по типам, которыми ещё занимаются, и по уже стоящим мастерским:
            // ремесло могло вымереть совсем, и тогда его мастерскую нужно доветшать,
            // а по одним только живым ремесленникам мы бы до неё не добрались
            var types = craftersByType.Keys.Union(settlement.Workshops.Keys).ToList();

            foreach (var type in types)
            {
                var targetWorkshops = craftersByType.GetValueOrDefault(type) / CraftersPerWorkshop;
                var current = settlement.Workshops.GetValueOrDefault(type);

                if (current >= targetWorkshops)
                {
                    if (DecaySystem.ShouldDecay(current, targetWorkshops, world))
                    {
                        settlement.Workshops[type] = current - 1; // Ремесленников не осталось — мастерская стоит без дела и ветшает
                    }

                    continue;
                }

                var stock = settlement.MaterialStocks.GetValueOrDefault(type);
                var cost = WorkshopCost * TechnologySystem.GetBuildCostMultiplier(world); // См. HousingSystem

                if (stock < cost)
                {
                    continue;
                }

                settlement.MaterialStocks[type] = stock - cost;
                settlement.Workshops[type] = current + 1;
            }
        }
    }

    // Множитель производства материала данного типа (1.0 — мастерских нет, выше — чем больше мастерских)
    public static double GetProductionMultiplier(Settlement? settlement, MaterialType type)
    {
        if (settlement == null)
        {
            return 1.0;
        }

        var count = settlement.Workshops.GetValueOrDefault(type);

        return 1 + Math.Min(MaxProductionBonus, count * BonusPerWorkshop);
    }
}
