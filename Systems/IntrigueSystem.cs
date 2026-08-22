using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Шпионаж между государствами. MurderSystem умеет только один вид заговора —
// внутри правящего дома, против собственного государя. Соперничество между
// коронами при этом уже есть в данных: то же спорное поселение, из-за
// которого WarSystem объявляет войну, — повод и для интриги, не только для
// осады.
//
// Шпиона в мире нет и не заводится — эту роль играет канцлер (см. CourtOffice.Chancellor),
// тот же советник, что уже двигает знание мира (TechnologySystem). Хитрость и
// осведомлённость — одно и то же умение, только приложенное не к книгам,
// а к казне соперника: канцлер тайно переводит часть чужой казны в свою,
// не доводя дело до открытой войны
public static class IntrigueSystem
{
    private const double EspionageChance = 0.15; // Шанс в год, что канцлер решится на интригу против одного из соперников
    private const double BaseStolenShare = 0.1; // Доля золотой казны соперника, уходящая при удачной интриге
    private const double StolenSharePerMastery = 0.05; // Опытный канцлер крадёт больше
    private const double MaxStolenShare = 0.3;

    public static void Process(World world)
    {
        var activeKingdoms = world.Kingdoms.Where(k => k.FallenYear == null).ToList();

        foreach (var spy in activeKingdoms)
        {
            if (!CourtSystem.HasOffice(spy, CourtOffice.Chancellor))
            {
                continue;
            }

            var target = activeKingdoms.FirstOrDefault(k => k != spy && AreRivals(spy, k));

            if (target == null || Rng.NextDouble() >= EspionageChance)
            {
                continue;
            }

            var strength = CourtSystem.GetOfficeStrength(spy, CourtOffice.Chancellor, world);
            var share = Math.Min(MaxStolenShare, BaseStolenShare + strength * StolenSharePerMastery);
            var stolen = target.GoldTreasury * share;

            if (stolen <= 0)
            {
                continue;
            }

            target.GoldTreasury -= stolen;
            spy.GoldTreasury += stolen;

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Espionage,
                Description = $"{spy.Name}: канцлер выведал и увёл {stolen:0.#} золота из казны {target.Name}",
                Kingdoms = [spy, target]
            });
        }
    }

    // Соперники — те же претенденты, из-за которых спорят поселения (см. WarSystem):
    // общее владение хотя бы одним поселением сразу у двух корон
    private static bool AreRivals(Kingdom a, Kingdom b)
    {
        return a.Settlements.Intersect(b.Settlements).Any();
    }
}
