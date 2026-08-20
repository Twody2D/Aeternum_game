using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Кого забирает война — правило, которое легко незаметно свести обратно
// к равновероятному жребию, поэтому проверяется и сама шкала риска,
// и распределение потерь на большой выборке
public class WarCasualtyTests
{
    private static Character Person(int id, LifeStage stage, Gender gender, string profession)
    {
        return new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = stage == LifeStage.Adult ? 30 : stage == LifeStage.Elder ? 70 : 10,
            Alive = true,
            LifeStage = stage,
            Gender = gender,
            Profession = profession
        };
    }

    private static List<Character> Town()
    {
        var people = new List<Character>();

        for (var i = 0; i < 10; i++)
        {
            people.Add(Person(i, LifeStage.Adult, Gender.Male, "Воин"));
            people.Add(Person(100 + i, LifeStage.Adult, Gender.Male, "Кузнец"));
            people.Add(Person(200 + i, LifeStage.Adult, Gender.Female, "Ткач"));
            people.Add(Person(300 + i, LifeStage.Student, Gender.Male, "Школьник"));
        }

        return people;
    }

    [Fact]
    public void GetWarRisk_SoldierRisksMostAndChildLeast()
    {
        var soldier = Person(1, LifeStage.Adult, Gender.Male, "Воин");
        var militia = Person(2, LifeStage.Adult, Gender.Male, "Кузнец");
        var woman = Person(3, LifeStage.Adult, Gender.Female, "Ткач");
        var child = Person(4, LifeStage.Child, Gender.Male, null!);

        Assert.True(WarSystem.GetWarRisk(soldier) > WarSystem.GetWarRisk(militia));
        Assert.True(WarSystem.GetWarRisk(militia) > WarSystem.GetWarRisk(woman));
        Assert.True(WarSystem.GetWarRisk(woman) > WarSystem.GetWarRisk(child));
    }

    [Fact]
    public void GetWarRisk_ElderMan_IsNotCountedAsMilitia()
    {
        // На стены сгоняют тех, кто может стоять на стенах
        var elder = Person(1, LifeStage.Elder, Gender.Male, "Кузнец");
        var adult = Person(2, LifeStage.Adult, Gender.Male, "Кузнец");

        Assert.True(WarSystem.GetWarRisk(adult) > WarSystem.GetWarRisk(elder));
    }

    [Fact]
    public void PickCasualties_TakesSoldiersFarMoreOftenThanChildren()
    {
        var people = Town();

        Rng.Initialize(seed: 1);

        var soldiers = 0;
        var children = 0;

        for (var battle = 0; battle < 500; battle++)
        {
            foreach (var casualty in WarSystem.PickCasualties(people, 8))
            {
                if (casualty.Profession == "Воин")
                {
                    soldiers++;
                }
                else if (casualty.LifeStage == LifeStage.Student)
                {
                    children++;
                }
            }
        }

        Assert.True(soldiers > children * 3, $"защитники обязаны нести основные потери, а вышло {soldiers} против {children}");
    }

    [Fact]
    public void PickCasualties_StillTouchesCivilians()
    {
        // Полная неприкосновенность гражданских была бы такой же неправдой,
        // как и жребий поровну: осада задевает весь город
        var people = Town();

        Rng.Initialize(seed: 1);

        var children = 0;

        for (var battle = 0; battle < 500; battle++)
        {
            children += WarSystem.PickCasualties(people, 8).Count(c => c.LifeStage == LifeStage.Student);
        }

        Assert.True(children > 0, "война добирается и до тех, кто не воюет");
    }

    [Fact]
    public void PickCasualties_TakesExactlyAsManyAsAsked()
    {
        var people = Town();

        Rng.Initialize(seed: 1);

        Assert.Equal(8, WarSystem.PickCasualties(people, 8).Count);
        Assert.Equal(people.Count, WarSystem.PickCasualties(people, people.Count * 2).Count);
        Assert.Empty(WarSystem.PickCasualties(people, 0));
    }

    [Fact]
    public void PickCasualties_NeverTakesTheSamePersonTwice()
    {
        var people = Town();

        Rng.Initialize(seed: 1);

        var casualties = WarSystem.PickCasualties(people, 20);

        Assert.Equal(casualties.Count, casualties.Distinct().Count());
    }
}
