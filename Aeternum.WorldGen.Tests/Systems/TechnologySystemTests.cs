using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

public class TechnologySystemTests
{
    private static Kingdom BuildKingdom(int id, bool hardworkingRuler)
    {
        var ruler = new Character
        {
            Id = id, Name = $"Правитель{id}", LastName = "Тестов", Age = 40,
            Alive = true, LifeStage = LifeStage.Adult
        };

        if (hardworkingRuler)
        {
            ruler.Traits.Add(Trait.Hardworking);
        }

        return new Kingdom
        {
            Id = id,
            Name = $"Королевство{id}",
            Dynasty = new Dynasty { Id = id, Name = $"Дом{id}", FoundedYear = 1, Founder = ruler },
            Ruler = ruler,
            FoundedYear = 1
        };
    }

    [Fact]
    public void Process_HardworkingRuler_ContributesExtraKnowledge()
    {
        var plain = new World();
        plain.Kingdoms.Add(BuildKingdom(1, hardworkingRuler: false));

        var patronized = new World();
        patronized.Kingdoms.Add(BuildKingdom(1, hardworkingRuler: true));

        TechnologySystem.Process(plain);
        TechnologySystem.Process(patronized);

        Assert.True(patronized.Knowledge > plain.Knowledge,
            $"усердный государь должен покровительствовать учёности: {patronized.Knowledge} против {plain.Knowledge}");
    }

    [Fact]
    public void Process_FallenKingdomsRulerTrait_DoesNotContribute()
    {
        var world = new World();
        var kingdom = BuildKingdom(1, hardworkingRuler: true);
        kingdom.FallenYear = 50;
        world.Kingdoms.Add(kingdom);

        TechnologySystem.Process(world);

        Assert.Equal(0, world.Knowledge);
    }

    [Fact]
    public void GetEraName_NoKnowledge_IsDarkAges()
    {
        Assert.Equal("Тёмные века", TechnologySystem.GetEraName(0));
    }

    [Fact]
    public void GetProductionMultiplier_DarkAges_ChangesNothing()
    {
        // Первая эпоха — то состояние, в котором мир жил до появления технологий:
        // её множитель обязан быть ровно единицей, иначе прежний баланс сместился бы
        Assert.Equal(1.0, TechnologySystem.GetProductionMultiplier(new World { Knowledge = 0 }));
    }

    [Fact]
    public void GetEraName_AdvancesWithKnowledge()
    {
        var early = TechnologySystem.GetEraName(0);
        var later = TechnologySystem.GetEraName(100_000);

        Assert.NotEqual(early, later);
    }

    [Fact]
    public void GetProductionMultiplier_NeverDecreasesWithKnowledge()
    {
        // Знание не убывает, и отдача от него тоже не должна проседать ни на одном пороге
        var previous = TechnologySystem.GetProductionMultiplier(new World { Knowledge = 0 });

        for (double knowledge = 0; knowledge <= 3000; knowledge += 25)
        {
            var current = TechnologySystem.GetProductionMultiplier(new World { Knowledge = knowledge });

            Assert.True(current >= previous, $"отдача просела на знании {knowledge}");
            previous = current;
        }
    }

    [Fact]
    public void GetEraName_HugeKnowledge_StaysAtLastEra()
    {
        // За последним порогом эпох больше нет — выбор не должен уходить за границы шкалы
        var last = TechnologySystem.GetEraName(1_000_000);
        var alsoLast = TechnologySystem.GetEraName(10_000_000);

        Assert.Equal(last, alsoLast);
    }

    [Fact]
    public void Process_ScholarsAndSchools_AccumulateKnowledge()
    {
        var world = new World();
        world.Settlements.Add(new Aeternum.WorldGen.Models.Settlement { Id = 1, Name = "Тестовка", Schools = 2 });

        TechnologySystem.Process(world);

        Assert.True(world.Knowledge > 0, "школы должны копить знание сами по себе");
    }

    [Fact]
    public void Process_EmptyWorld_KeepsKnowledgeIntact()
    {
        // Знание не забывается, даже когда думать стало некому
        var world = new World { Knowledge = 500 };

        TechnologySystem.Process(world);

        Assert.Equal(500, world.Knowledge);
    }
}
