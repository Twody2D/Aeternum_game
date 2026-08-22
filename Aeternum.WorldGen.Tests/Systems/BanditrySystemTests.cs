using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Разбой на большой дороге — оборотная сторона тирании расстояния
// (см. CapitalSystem.GetControl): дальняя от престола земля рискует
// золотом, столица — никогда; свой гарнизон снижает риск, но не убирает
public class BanditrySystemTests
{
    private static (World World, Kingdom Kingdom) BuildRealm()
    {
        var world = new World { CurrentYear = 100 };
        var capital = new Settlement { Id = 1, Name = "Столица", X = 0, Y = 0, Gold = 1000 };
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult, Settlement = capital };
        capital.Members.Add(ruler);
        world.Characters.Add(ruler);
        world.Settlements.Add(capital);

        var kingdom = new Kingdom
        {
            Id = 1, Name = "Королевство Тестов", FoundedYear = 1, Ruler = ruler,
            Dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 },
            Settlements = [capital],
            Capital = capital
        };

        world.Kingdoms.Add(kingdom);

        return (world, kingdom);
    }

    private static Settlement AddFarSettlement(World world, Kingdom kingdom, double gold)
    {
        var settlement = new Settlement { Id = world.Settlements.Count + 1, Name = "Окраина", X = ClimateSystem.MapSize, Y = ClimateSystem.MapSize, Gold = gold };
        world.Settlements.Add(settlement);
        kingdom.Settlements.Add(settlement);

        return settlement;
    }

    [Fact]
    public void Process_Capital_IsNeverRobbed()
    {
        for (var run = 0; run < 200; run++)
        {
            var (world, _) = BuildRealm();

            Rng.Initialize(seed: run + 1);
            BanditrySystem.Process(world);

            Assert.Equal(1000, world.Settlements[0].Gold);
        }
    }

    [Fact]
    public void Process_FarUndefendedSettlement_IsSometimesRobbed()
    {
        var robbed = false;
        World? lastWorld = null;

        for (var run = 0; run < 300 && !robbed; run++)
        {
            var (world, kingdom) = BuildRealm();
            var outpost = AddFarSettlement(world, kingdom, gold: 1000);
            lastWorld = world;

            Rng.Initialize(seed: run + 1);
            BanditrySystem.Process(world);

            robbed = outpost.Gold < 1000;
        }

        Assert.True(robbed, "хотя бы раз за 300 попыток дальняя незащищённая земля должна лишиться золота");
        Assert.Contains(lastWorld!.Events, e => e.Type == EventType.Banditry);
    }

    [Fact]
    public void Process_StrongGarrison_IsRobbedLessOftenThanUndefended()
    {
        var undefended = CountRobberies(garrisonSize: 0);
        var defended = CountRobberies(garrisonSize: 10);

        Assert.True(defended < undefended,
            $"сильный гарнизон должен снижать частоту разбоя: {defended} против {undefended}");
    }

    private static int CountRobberies(int garrisonSize)
    {
        var robberies = 0;

        for (var run = 0; run < 300; run++)
        {
            var (world, kingdom) = BuildRealm();
            var outpost = AddFarSettlement(world, kingdom, gold: 1000);

            for (var i = 0; i < garrisonSize; i++)
            {
                var soldier = new Character
                {
                    Id = 100 + i, Name = $"Воин{i}", LastName = "Тестов", Age = 30, Alive = true,
                    LifeStage = LifeStage.Adult, Settlement = outpost, Profession = "Воин", ProfessionYear = 50
                };
                outpost.Members.Add(soldier);
                world.Characters.Add(soldier);
            }

            Rng.Initialize(seed: run + 1);
            BanditrySystem.Process(world);

            if (outpost.Gold < 1000)
            {
                robberies++;
            }
        }

        return robberies;
    }

    [Fact]
    public void Process_NoGold_IsNeverRobbed()
    {
        for (var run = 0; run < 200; run++)
        {
            var (world, kingdom) = BuildRealm();
            var outpost = AddFarSettlement(world, kingdom, gold: 0);

            Rng.Initialize(seed: run + 1);
            BanditrySystem.Process(world);

            Assert.Equal(0, outpost.Gold);
        }
    }
}
