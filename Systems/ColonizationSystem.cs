using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Generators;

namespace Aeternum.WorldGen.Systems;

// Рост населения расширяет территорию: переполненное поселение с накопленными
// материалами (первое реальное применение MaterialStocks) и с некоторым шансом
// отпускает одну семью основать новое поселение — иначе число поселений навсегда
// остаётся тем, с которого начался мир (ProjectSettings.SettlementCount)
public static class ColonizationSystem
{
    public static void Process(World world)
    {
        // ToList — Process может добавить новые поселения прямо в world.Settlements,
        // а по ним самим колонизацию в этот же год не проверяем
        foreach (var origin in world.Settlements.ToList())
        {
            var alivePopulation = origin.Members.Count(m => m.Alive);

            if (alivePopulation < world.Settings.ColonizationPopulationThreshold)
            {
                continue;
            }

            if (origin.MaterialStocks.Values.Sum() < world.Settings.ColonizationMaterialCost)
            {
                continue; // Не на что снарядить колонистов
            }

            if (Rng.NextDouble() >= world.Settings.ColonizationChance)
            {
                continue;
            }

            var candidateFamilies = world.Families
                .Where(f =>
                    f.Father.Alive && f.Father.Settlement == origin &&
                    f.Mother.Alive && f.Mother.Settlement == origin)
                .ToList();

            if (candidateFamilies.Count == 0)
            {
                continue; // Некому основывать — в этом году колонизации не будет
            }

            var foundingFamily = candidateFamilies[Rng.Next(candidateFamilies.Count)];

            Found(origin, foundingFamily, world);
        }
    }

    private static void Found(Settlement origin, Family foundingFamily, World world)
    {
        // Списываем пропорционально по всем типам материалов, а не только один конкретный тип
        var totalStock = origin.MaterialStocks.Values.Sum();
        var remainingRatio = 1 - world.Settings.ColonizationMaterialCost / totalStock;

        foreach (var type in origin.MaterialStocks.Keys.ToList())
        {
            origin.MaterialStocks[type] *= remainingRatio;
        }

        var newSettlement = SettlementGenerator.Create(1, origin)[0];

        newSettlement.Culture = origin.Culture;
        newSettlement.Religion = origin.Religion;

        world.Settlements.Add(newSettlement);

        var colonists = new List<Character> { foundingFamily.Father, foundingFamily.Mother };
        colonists.AddRange(foundingFamily.Children.Where(c => c.Alive));

        foreach (var colonist in colonists)
        {
            colonist.Settlement = newSettlement;
            newSettlement.Members.Add(colonist);
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Colonization,
            Description = $"{origin.Name} разрослось: семья {SurnameSystem.GetDisplayFullName(foundingFamily.Father)} " +
                          $"и {SurnameSystem.GetDisplayFullName(foundingFamily.Mother)} основала {newSettlement.Name}"
        });
    }
}
