using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Катастрофы — редкие резкие потрясения поселения, в отличие от плавной
// вероятности обычной смертности/голода. Неурожай не убивает напрямую — только
// выбивает запас еды, а голодную цепочку последствий доводит до конца уже
// существующий EconomySystem (см. её описание), новой логики голода тут нет
public static class DisasterSystem
{
    private static readonly Random _random = new();

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var residents = settlement.Members.Where(m => m.Alive).ToList();

            if (residents.Count == 0)
            {
                continue;
            }

            if (_random.NextDouble() >= world.Settings.DisasterChance)
            {
                continue;
            }

            if (_random.Next(2) == 0)
            {
                TriggerEpidemic(settlement, residents, world);
            }
            else
            {
                TriggerCropFailure(settlement, residents, world);
            }
        }
    }

    private static void TriggerEpidemic(Settlement settlement, List<Character> residents, World world)
    {
        var effectiveMortalityRate = world.Settings.EpidemicMortalityRate * HospitalSystem.GetHospitalFactor(settlement);
        var casualtyCount = (int)(residents.Count * effectiveMortalityRate);

        var casualties = residents
            .OrderBy(_ => _random.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.Disease);
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: эпидемия, погибших — {casualties.Count}"
        });
    }

    private static void TriggerCropFailure(Settlement settlement, List<Character> residents, World world)
    {
        var loss = residents.Count * world.Settings.FoodConsumptionPerCapita * world.Settings.CropFailureFoodLossFactor;

        settlement.FoodStock -= loss;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Disaster,
            Description = $"{settlement.Name}: неурожай, потеряно {loss:F0} запаса еды"
        });
    }
}
