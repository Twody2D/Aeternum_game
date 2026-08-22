using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Шпионаж — интрига за пределами одного заговора (см. MurderSystem): не
// внутри дома, а между державами, и не убийство, а кража казны. Проверяется,
// что канцлер нужен, что соперничество определяется тем же общим поселением,
// что и войну (см. WarSystem), и что опытный канцлер крадёт больше
public class IntrigueSystemTests
{
    private static int _nextId = 1;

    private static Kingdom BuildKingdom(Settlement seat, double goldTreasury = 0)
    {
        var ruler = new Character
        {
            Id = _nextId++, Name = "Правитель", LastName = "Тестов", Age = 45,
            Alive = true, LifeStage = LifeStage.Adult, Settlement = seat
        };
        seat.Members.Add(ruler);

        return new Kingdom
        {
            Id = _nextId++,
            Name = $"Королевство{_nextId}",
            Dynasty = new Dynasty { Id = _nextId, Name = $"Дом{_nextId}", FoundedYear = 1, Founder = ruler },
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat,
            GoldTreasury = goldTreasury
        };
    }

    private static Character AppointChancellor(Kingdom kingdom, Settlement settlement, int professionYear = 50)
    {
        var chancellor = new Character
        {
            Id = _nextId++, Name = "Канцлер", LastName = "Тестов", Age = 50,
            Alive = true, LifeStage = LifeStage.Adult, Settlement = settlement,
            Profession = "Учёный", ProfessionYear = professionYear
        };

        settlement.Members.Add(chancellor);
        kingdom.Court[CourtOffice.Chancellor] = chancellor;

        return chancellor;
    }

    [Fact]
    public void Process_ChancellorAgainstRival_StealsGold()
    {
        var world = new World { CurrentYear = 100 };
        var disputed = new Settlement { Id = 1, Name = "Спорная" };
        world.Settlements.Add(disputed);

        var seatA = new Settlement { Id = 2, Name = "СтолицаА" };
        var seatB = new Settlement { Id = 3, Name = "СтолицаБ" };
        world.Settlements.Add(seatA);
        world.Settlements.Add(seatB);

        var spy = BuildKingdom(seatA);
        spy.Settlements.Add(disputed);
        AppointChancellor(spy, seatA);

        var target = BuildKingdom(seatB, goldTreasury: 100);
        target.Settlements.Add(disputed);

        world.Kingdoms.Add(spy);
        world.Kingdoms.Add(target);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 50 && target.GoldTreasury >= 100; year++)
        {
            world.CurrentYear = 100 + year;
            IntrigueSystem.Process(world);
        }

        Assert.True(target.GoldTreasury < 100, "казна соперника должна была оскудеть от интриг");
        Assert.True(spy.GoldTreasury > 0, "уведённое золото обязано осесть у шпиона");
        Assert.Contains(world.Events, e => e.Type == EventType.Espionage);
    }

    [Fact]
    public void Process_WithoutChancellor_NoEspionage()
    {
        var world = new World { CurrentYear = 100 };
        var disputed = new Settlement { Id = 1, Name = "Спорная" };
        world.Settlements.Add(disputed);

        var seatA = new Settlement { Id = 2, Name = "СтолицаА" };
        var seatB = new Settlement { Id = 3, Name = "СтолицаБ" };
        world.Settlements.Add(seatA);
        world.Settlements.Add(seatB);

        var a = BuildKingdom(seatA);
        a.Settlements.Add(disputed);

        var b = BuildKingdom(seatB, goldTreasury: 100);
        b.Settlements.Add(disputed);

        world.Kingdoms.Add(a);
        world.Kingdoms.Add(b);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 50; year++)
        {
            world.CurrentYear = 100 + year;
            IntrigueSystem.Process(world);
        }

        Assert.Equal(100, b.GoldTreasury);
        Assert.DoesNotContain(world.Events, e => e.Type == EventType.Espionage);
    }

    [Fact]
    public void Process_KingdomsWithoutSharedSettlement_AreNotRivals()
    {
        // Канцлер есть, но соперничать не с кем — общего поселения нет
        var world = new World { CurrentYear = 100 };

        var seatA = new Settlement { Id = 1, Name = "СтолицаА" };
        var seatB = new Settlement { Id = 2, Name = "СтолицаБ" };
        world.Settlements.Add(seatA);
        world.Settlements.Add(seatB);

        var spy = BuildKingdom(seatA);
        AppointChancellor(spy, seatA);

        var stranger = BuildKingdom(seatB, goldTreasury: 100);

        world.Kingdoms.Add(spy);
        world.Kingdoms.Add(stranger);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 50; year++)
        {
            world.CurrentYear = 100 + year;
            IntrigueSystem.Process(world);
        }

        Assert.Equal(100, stranger.GoldTreasury);
    }

    [Fact]
    public void Process_MoreExperiencedChancellor_StealsALargerShare()
    {
        var noviceStolenShare = StolenShareOver(professionYear: 100);
        var masterStolenShare = StolenShareOver(professionYear: 40);

        Assert.True(masterStolenShare > noviceStolenShare,
            $"опытный канцлер должен красть больше: {masterStolenShare} против {noviceStolenShare}");
    }

    private static double StolenShareOver(int professionYear)
    {
        var world = new World { CurrentYear = 100 };
        var disputed = new Settlement { Id = 1, Name = "Спорная" };
        world.Settlements.Add(disputed);

        var seatA = new Settlement { Id = 2, Name = "СтолицаА" };
        var seatB = new Settlement { Id = 3, Name = "СтолицаБ" };
        world.Settlements.Add(seatA);
        world.Settlements.Add(seatB);

        var spy = BuildKingdom(seatA);
        spy.Settlements.Add(disputed);
        AppointChancellor(spy, seatA, professionYear);

        var target = BuildKingdom(seatB, goldTreasury: 1000);
        target.Settlements.Add(disputed);

        world.Kingdoms.Add(spy);
        world.Kingdoms.Add(target);

        Rng.Initialize(seed: 1);

        // Один успешный акт интриги и один процент кражи с него — дальше не идём,
        // иначе накопительный эффект по годам смажет разницу в самой доле
        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            var before = target.GoldTreasury;

            IntrigueSystem.Process(world);

            if (target.GoldTreasury < before)
            {
                return (before - target.GoldTreasury) / before;
            }
        }

        throw new InvalidOperationException("интрига ни разу не удалась за 200 лет — тест нужно пересмотреть");
    }
}
