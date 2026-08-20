using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Завершает экономический год: запасать бесконечно нельзя. Зерно гниёт само
// по себе, а всё, чему не хватило места на складе, пропадает — под открытым
// небом, в подтопленной яме, растащенным. Вместимость не задаётся отдельным
// полем, а следует из уже стоящих построек: еду хранят по домам, сырьё —
// при мастерских своего ремесла, казну — по подконтрольным поселениям.
// Отсюда же и разница между товарами: золото не гниёт и места почти не
// занимает, поэтому оно единственное копится без предела — торговое поселение
// объективно устойчивее ремесленного, хотя нигде это правило не записано.
// Ничего не логирует — по тому же принципу, что EconomySystem и TradeSystem:
// событие мира это заметное последствие (голод), а не сам перерасчёт
public static class StorageSystem
{
    private const double BaseFoodCapacity = 25; // Яма и пара амбаров есть даже там, где домов ещё нет
    private const double FoodCapacityPerHouse = 30;

    // Держим выше стоимости самой дорогой единичной траты (колонизация — 50
    // материалов), иначе поселение физически не смогло бы накопить на неё
    private const double BaseMaterialCapacity = 60;
    private const double MaterialCapacityPerWorkshop = 40;

    private const double TreasuryCapacityPerSettlement = 300; // Казна хранится не в одном месте, а по всем городам короны

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            settlement.FoodStock = Settle(settlement.FoodStock, GetFoodCapacity(settlement), world, spoils: true);

            foreach (var type in settlement.MaterialStocks.Keys.ToList())
            {
                settlement.MaterialStocks[type] = Settle(
                    settlement.MaterialStocks[type],
                    GetMaterialCapacity(settlement, type),
                    world,
                    spoils: false);
            }
        }

        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            var capacity = GetTreasuryCapacity(kingdom);

            kingdom.FoodTreasury = Settle(kingdom.FoodTreasury, capacity, world, spoils: true);

            foreach (var type in kingdom.MaterialTreasury.Keys.ToList())
            {
                kingdom.MaterialTreasury[type] = Settle(kingdom.MaterialTreasury[type], capacity, world, spoils: false);
            }
        }
    }

    // Сколько еды поселение способно сохранить до следующего года
    public static double GetFoodCapacity(Settlement settlement)
    {
        return BaseFoodCapacity + FoodCapacityPerHouse * settlement.Houses;
    }

    // Сырьё лежит при мастерской своего ремесла: кузница расширяет склад металла, но не тканей
    public static double GetMaterialCapacity(Settlement settlement, MaterialType type)
    {
        return BaseMaterialCapacity + MaterialCapacityPerWorkshop * settlement.Workshops.GetValueOrDefault(type);
    }

    // Казне негде лежать, кроме как в городах короны: потеряв земли, государство теряет и способность запасать
    public static double GetTreasuryCapacity(Kingdom kingdom)
    {
        return TreasuryCapacityPerSettlement * kingdom.Settlements.Count;
    }

    // Дефицит (отрицательный запас) не трогаем: это долг перед жителями,
    // с ним разбирается EconomySystem, а гнить в пустом амбаре нечему
    private static double Settle(double stock, double capacity, World world, bool spoils)
    {
        if (stock <= 0)
        {
            return stock;
        }

        if (spoils)
        {
            stock -= stock * world.Settings.FoodSpoilageRate;
        }

        if (stock <= capacity)
        {
            return stock;
        }

        return capacity + (stock - capacity) * (1 - world.Settings.StorageOverflowLossRate);
    }
}
