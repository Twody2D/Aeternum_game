using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Надбавка гильдии (см. GuildSystem.GetQualityPremium) — постоянная и тихая:
// цех умелых мастеров просто продаёт дороже, год за годом. Шедевр — редкая,
// разовая награда за выучку, а не постоянная прибавка: глава цеха
// (GuildSystem.GetGuildmaster), достигший предела мастерства (см.
// ProfessionSystem.GetMastery — дальше учиться уже некуда), изредка создаёт
// вещь, за которую платят не по весу сырья, а по имени мастера — разовый
// золотой куш поселению и легенда, тот же счётчик, что уже копит долгожителей
// (см. Settlement.LegendCount, DeathSystem)
public static class MasterworkSystem
{
    private const double MasterworkChance = 0.002; // Редкость события — на весь мир, не на одного мастера
    private const double MasterworkGoldReward = 60;

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            foreach (var type in Enum.GetValues<MaterialType>())
            {
                var guildmaster = GuildSystem.GetGuildmaster(settlement, type, world);

                if (guildmaster == null || !IsAtPeakMastery(guildmaster, world))
                {
                    continue;
                }

                if (Rng.NextDouble() >= MasterworkChance)
                {
                    continue;
                }

                settlement.Gold += MasterworkGoldReward;
                settlement.LegendCount++;

                world.Events.Add(new WorldEvent
                {
                    Year = world.CurrentYear,
                    Type = EventType.Masterwork,
                    Description = $"{settlement.Name}: {SurnameSystem.GetDisplayFullName(guildmaster)} создал(а) шедевр — " +
                                  $"поселение выручило {MasterworkGoldReward:F0} золота"
                });
            }
        }
    }

    private static bool IsAtPeakMastery(Character master, World world)
    {
        return ProfessionSystem.GetMastery(master, world) >= ProfessionSystem.MaxMastery;
    }
}
