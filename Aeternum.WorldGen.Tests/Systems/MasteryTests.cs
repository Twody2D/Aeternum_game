using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Мастерство нигде не хранится — оно выводится из возраста, и именно поэтому
// его легко потерять при правках. Проверяется и сама шкала, и то, ради чего
// она заведена: возраст расцвета работника
public class MasteryTests
{
    private const int AdultAge = 18;

    // Стаж отсчитывается от года, когда взялись за дело (см. Character.ProfessionYear).
    // В мире, стоящем на нулевом году, столько же лет опыта даёт отрицательный год начала
    private static Character Worker(int id, int age, string profession = "Фермер")
    {
        return new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = age >= 60 ? LifeStage.Elder : age >= 16 ? LifeStage.Adult : LifeStage.Student,
            Profession = profession,
            ProfessionYear = -Math.Max(0, age - AdultAge)
        };
    }

    private static World WorldAtYearZero()
    {
        return new World();
    }

    private static World WorldOfWorkers(int count, int age)
    {
        var world = new World();
        var settlement = new Settlement { Id = 1, Name = "Тестовка", X = 500, Y = 500 };
        world.Settlements.Add(settlement);

        for (var i = 0; i < count; i++)
        {
            var worker = Worker(i + 1, age);
            worker.Settlement = settlement;
            settlement.Members.Add(worker);
            world.Characters.Add(worker);
        }

        return world;
    }

    [Fact]
    public void GetMastery_Novice_HasNoBonus()
    {
        var world = WorldAtYearZero();

        Assert.Equal(1.0, ProfessionSystem.GetMastery(Worker(1, AdultAge), world));
    }

    [Fact]
    public void GetMastery_GrowsWithYearsInTrade()
    {
        var world = WorldAtYearZero();

        Assert.True(ProfessionSystem.GetMastery(Worker(1, 40), world)
                    > ProfessionSystem.GetMastery(Worker(2, 25), world));
    }

    [Fact]
    public void GetMastery_StopsAtCeiling()
    {
        // Иначе столетний старик оказался бы вдвое полезнее зрелого мастера
        var world = WorldAtYearZero();

        Assert.Equal(ProfessionSystem.GetMastery(Worker(1, 60), world),
                     ProfessionSystem.GetMastery(Worker(2, 99), world));
    }

    [Fact]
    public void GetMastery_WithoutProfession_IsNeutral()
    {
        var world = WorldAtYearZero();
        var idler = Worker(1, 50);
        idler.Profession = null;

        Assert.Equal(1.0, ProfessionSystem.GetMastery(idler, world));
    }

    [Fact]
    public void GetMastery_Child_IsNeutral()
    {
        // Стаж считается от совершеннолетия, и до него он не может быть отрицательным
        var world = WorldAtYearZero();

        Assert.Equal(1.0, ProfessionSystem.GetMastery(Worker(1, 10, "Школьник"), world));
    }

    [Fact]
    public void EconomyProcess_ExperiencedWorkers_FeedBetterThanYoungOnes()
    {
        var young = WorldOfWorkers(count: 10, age: 18);
        var seasoned = WorldOfWorkers(count: 10, age: 45);

        EconomySystem.Process(young);
        EconomySystem.Process(seasoned);

        Assert.True(seasoned.Settlements[0].FoodStock > young.Settlements[0].FoodStock,
            "опытные работники обязаны кормить поселение лучше вчерашних учеников");
    }

    [Fact]
    public void EconomyProcess_PrimeOfLifeBeatsBothYouthAndOldAge()
    {
        // Ради этого мастерство и заводилось: юнец силён, да неумел, старик
        // умел, да немощен — лучше всех работает тот, у кого есть и то и другое
        var young = WorldOfWorkers(count: 10, age: 18);
        var prime = WorldOfWorkers(count: 10, age: 50);
        var old = WorldOfWorkers(count: 10, age: 75);

        EconomySystem.Process(young);
        EconomySystem.Process(prime);
        EconomySystem.Process(old);

        Assert.True(prime.Settlements[0].FoodStock > young.Settlements[0].FoodStock);
        Assert.True(prime.Settlements[0].FoodStock > old.Settlements[0].FoodStock);
    }

    [Fact]
    public void TechnologyProcess_SeasonedScholarsAdvanceTheWorldFaster()
    {
        var young = WorldOfWorkers(count: 10, age: 18);
        var seasoned = WorldOfWorkers(count: 10, age: 55);

        foreach (var world in new[] { young, seasoned })
        {
            foreach (var scholar in world.Characters)
            {
                scholar.Profession = "Учёный";
            }
        }

        TechnologySystem.Process(young);
        TechnologySystem.Process(seasoned);

        Assert.True(seasoned.Knowledge > young.Knowledge, "седой книжник продвигает мир дальше вчерашнего школяра");
    }
}
