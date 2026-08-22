using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Шедевр — редкая награда мастеру на пике выучки (ProfessionSystem.MaxMastery),
// не постоянная надбавка вроде GuildSystem.GetQualityPremium: разовый золотой
// куш и легенда поселению, проверяется на выборке — один бросок ничего не решает
public class MasterworkSystemTests
{
    private static Settlement BuildSettlement(World world, int professionYear)
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовое" };
        var master = new Character
        {
            Id = 1, Name = "Мастер", LastName = "Тестов", Age = 50, Alive = true,
            LifeStage = LifeStage.Adult, Settlement = settlement,
            Profession = "Кузнец", ProfessionYear = professionYear
        };

        settlement.Members.Add(master);
        world.Characters.Add(master);
        world.Settlements.Add(settlement);

        return settlement;
    }

    [Fact]
    public void Process_MasterAtPeakMastery_SometimesCreatesMasterwork()
    {
        var created = false;

        for (var run = 0; run < 3000 && !created; run++)
        {
            var world = new World { CurrentYear = 100 };
            BuildSettlement(world, professionYear: 0); // Стаж с года 0 до года 100 — далеко за потолком мастерства

            Rng.Initialize(seed: run + 1);
            MasterworkSystem.Process(world);

            created = world.Events.Any(e => e.Type == EventType.Masterwork);
        }

        Assert.True(created, "хотя бы раз за 3000 попыток мастер на пике выучки должен создать шедевр");
    }

    [Fact]
    public void Process_MasterAtPeakMastery_GrantsGoldAndLegend()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = BuildSettlement(world, professionYear: 0);
        var goldBefore = settlement.Gold;
        var legendBefore = settlement.LegendCount;

        var gained = false;

        for (var run = 0; run < 3000 && !gained; run++)
        {
            Rng.Initialize(seed: run + 1);
            MasterworkSystem.Process(world);

            gained = settlement.Gold > goldBefore;
        }

        Assert.True(gained);
        Assert.True(settlement.LegendCount > legendBefore);
    }

    [Fact]
    public void Process_FreshMaster_NeverCreatesMasterwork()
    {
        for (var run = 0; run < 300; run++)
        {
            var world = new World { CurrentYear = 100 };
            BuildSettlement(world, professionYear: 100); // Только взялся за дело — стажа нет вовсе

            Rng.Initialize(seed: run + 1);
            MasterworkSystem.Process(world);

            Assert.DoesNotContain(world.Events, e => e.Type == EventType.Masterwork);
        }
    }

    [Fact]
    public void Process_NoGuildAtAll_NeverCreatesMasterwork()
    {
        for (var run = 0; run < 50; run++)
        {
            var world = new World { CurrentYear = 100 };
            world.Settlements.Add(new Settlement { Id = 1, Name = "Пустое" });

            Rng.Initialize(seed: run + 1);
            MasterworkSystem.Process(world);

            Assert.Empty(world.Events);
        }
    }
}
