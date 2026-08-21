using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// "Пастырь" был decorативной Knowledge-профессией без единого игрового следствия.
// Проверяется само распознавание сана, выбор духовного главы среди нескольких
// пастырей и то, что паства держится крепче под опытным духовенством
public class ClergySystemTests
{
    private static int _nextId = 1;

    private static Character MakeClergy(Settlement settlement, int professionYear, World world)
    {
        var character = new Character
        {
            Id = _nextId++,
            Name = $"Пастырь{_nextId}",
            LastName = "Тестов",
            Age = 40,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = "Пастырь",
            ProfessionYear = professionYear
        };

        settlement.Members.Add(character);

        return character;
    }

    [Fact]
    public void IsClergy_RecognisesOnlyThePriesthood()
    {
        Assert.True(ProfessionSystem.IsClergy("Пастырь"));
        Assert.False(ProfessionSystem.IsClergy("Фермер"));
        Assert.False(ProfessionSystem.IsClergy(null));
    }

    [Fact]
    public void GetSpiritualHead_EmptySettlement_IsNull()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };

        Assert.Null(ClergySystem.GetSpiritualHead(settlement, world));
    }

    [Fact]
    public void GetSpiritualHead_PicksTheMostMasterfulPastor()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };

        var novice = MakeClergy(settlement, professionYear: 95, world);
        var elder = MakeClergy(settlement, professionYear: 40, world);

        Assert.Equal(elder, ClergySystem.GetSpiritualHead(settlement, world));
        Assert.NotEqual(novice, ClergySystem.GetSpiritualHead(settlement, world));
    }

    [Fact]
    public void GetCohesionFactor_WithoutClergy_IsNeutral()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };

        Assert.Equal(1.0, ClergySystem.GetCohesionFactor(settlement, world));
    }

    [Fact]
    public void GetCohesionFactor_MoreExperiencedClergy_HoldsFlockTighter()
    {
        var world = new World { CurrentYear = 100 };

        var freshFlock = new Settlement { Id = 1, Name = "Молодая паства" };
        MakeClergy(freshFlock, professionYear: 100, world);

        var seasonedFlock = new Settlement { Id = 2, Name = "Устоявшаяся паства" };
        MakeClergy(seasonedFlock, professionYear: 40, world);

        var freshFactor = ClergySystem.GetCohesionFactor(freshFlock, world);
        var seasonedFactor = ClergySystem.GetCohesionFactor(seasonedFlock, world);

        Assert.True(seasonedFactor < freshFactor, $"опытный пастырь должен держать паству крепче: {seasonedFactor} против {freshFactor}");
        Assert.True(seasonedFactor < 1.0);
    }

    [Fact]
    public void SchismProcess_SettlementWithClergy_SchismsLessOften()
    {
        // Проверяется не сама шкала сплочённости, а то, что раскол её слушает
        var withClergy = CountSchisms(hasClergy: true);
        var withoutClergy = CountSchisms(hasClergy: false);

        Assert.True(withClergy < withoutClergy,
            $"духовенство должно снижать число расколов: {withClergy} против {withoutClergy}");
    }

    private static int CountSchisms(bool hasClergy)
    {
        var schisms = 0;

        for (var run = 0; run < 200; run++)
        {
            var world = new World { CurrentYear = 100 };
            var settlement = new Settlement { Id = 1, Name = "Паства", Religion = new Religion { Id = 1, Name = "Вера" } };

            // Разрыв с властью уже случился — раскол зависит только от того,
            // выстоит ли паства под его давлением
            settlement.RebellingUntilYear = 10_000;

            for (var i = 0; i < 15; i++)
            {
                settlement.Members.Add(new Character
                {
                    Id = i + 1,
                    Name = $"Житель{i}",
                    LastName = "Тестов",
                    Age = 30,
                    Alive = true,
                    LifeStage = LifeStage.Adult,
                    Settlement = settlement,
                    Profession = "Фермер"
                });
            }

            if (hasClergy)
            {
                MakeClergy(settlement, professionYear: 40, world);
            }

            world.Settlements.Add(settlement);

            Rng.Initialize(seed: run + 1);
            SchismSystem.Process(world);

            if (settlement.Religion?.Name != "Вера")
            {
                schisms++;
            }
        }

        return schisms;
    }
}
