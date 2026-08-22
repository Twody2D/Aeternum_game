using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Разбой на большой дороге — оборотная сторона тирании расстояния (см.
// CapitalSystem.GetControl): чем дальше земля от престола, тем хуже её
// стережёт не только сборщик дани (TributeSystem), но и большая дорога,
// по которой идёт золото с внешнего рынка (MarketSystem.SellSurplus).
// Своя защита не убирает разбой совсем, но заметно снижает шанс — гарнизон
// (ArmySystem) значит для дороги то же самое, что и для осады
public static class BanditrySystem
{
    private const double MaxRobberyChance = 0.25; // Потолок шанса при полной беззащитности самой дальней земли
    private const double GarrisonProtection = 0.05; // Каждая единица умения гарнизона снижает шанс
    private const double MaxGarrisonProtection = 0.6; // Полностью разбой не снять даже сильным гарнизоном
    private const double StolenShare = 0.3; // Доля золота поселения, уносимая за один набег

    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            foreach (var settlement in kingdom.Settlements)
            {
                if (settlement.Gold <= 0 || RebellionSystem.IsRebelling(settlement, world))
                {
                    continue;
                }

                var remoteness = 1 - CapitalSystem.GetControl(kingdom, settlement);
                var protection = Math.Min(MaxGarrisonProtection, ArmySystem.GetGarrisonStrength(settlement, world) * GarrisonProtection);
                var chance = remoteness * MaxRobberyChance * (1 - protection);

                if (Rng.NextDouble() >= chance)
                {
                    continue;
                }

                var stolen = settlement.Gold * StolenShare;
                settlement.Gold -= stolen;

                world.Events.Add(new WorldEvent
                {
                    Year = world.CurrentYear,
                    Type = EventType.Banditry,
                    Description = $"{settlement.Name}: разбойники на большой дороге унесли {stolen:F0} золота",
                    Kingdoms = [kingdom]
                });
            }
        }
    }
}
