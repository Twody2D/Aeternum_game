using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Взаимная склонность решает и с кем человек пойдёт под венец, и удержится ли
// потом семья. Проверяется сама шкала, отбор пары по ней и её действие
// на развод
public class AffinityTests
{
    private static Character Person(int id, Gender gender, int age = 25, params Trait[] traits)
    {
        var character = new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Gender = gender,
            Profession = "Фермер"
        };

        foreach (var trait in traits)
        {
            character.Traits.Add(trait);
        }

        return character;
    }

    private static void MakeFriends(Character a, Character b)
    {
        a.Friends.Add(b);
        b.Friends.Add(a);
    }

    [Fact]
    public void GetAffinity_Friends_AreDrawnToEachOther()
    {
        var man = Person(1, Gender.Male);
        var friend = Person(2, Gender.Female);
        var stranger = Person(3, Gender.Female);

        MakeFriends(man, friend);

        Assert.True(MarriageSystem.GetAffinity(man, friend) > MarriageSystem.GetAffinity(man, stranger));
    }

    [Fact]
    public void GetAffinity_SharedTraits_DrawTogether()
    {
        var man = Person(1, Gender.Male, 25, Trait.Brave, Trait.Hardworking);
        var alike = Person(2, Gender.Female, 25, Trait.Brave, Trait.Hardworking);
        var unlike = Person(3, Gender.Female, 25, Trait.Frail);

        Assert.True(MarriageSystem.GetAffinity(man, alike) > MarriageSystem.GetAffinity(man, unlike));
    }

    [Fact]
    public void GetAffinity_SharedCircleOfFriends_DrawsTogether()
    {
        // Знакомство через общий круг — тот же довод, что и прямая дружба, но слабее
        var man = Person(1, Gender.Male);
        var woman = Person(2, Gender.Female);
        var stranger = Person(3, Gender.Female);

        for (var i = 0; i < 3; i++)
        {
            var mutual = Person(10 + i, Gender.Male);
            MakeFriends(man, mutual);
            MakeFriends(woman, mutual);
        }

        Assert.True(MarriageSystem.GetAffinity(man, woman) > MarriageSystem.GetAffinity(man, stranger));
    }

    [Fact]
    public void GetAffinity_DeadMutualFriends_DoNotCount()
    {
        // Общий круг — это живые знакомые, а не список ушедших
        var man = Person(1, Gender.Male);
        var woman = Person(2, Gender.Female);
        var stranger = Person(3, Gender.Female);

        var ghost = Person(10, Gender.Male);
        ghost.Alive = false;

        MakeFriends(man, ghost);
        MakeFriends(woman, ghost);

        Assert.Equal(MarriageSystem.GetAffinity(man, stranger), MarriageSystem.GetAffinity(man, woman));
    }

    [Fact]
    public void GetAffinity_AgeGap_PushesApart()
    {
        var man = Person(1, Gender.Male, age: 25);
        var peer = Person(2, Gender.Female, age: 26);
        var elder = Person(3, Gender.Female, age: 45);

        Assert.True(MarriageSystem.GetAffinity(man, peer) > MarriageSystem.GetAffinity(man, elder));
    }

    [Fact]
    public void GetAffinity_StaysWithinBounds()
    {
        // Ни предрешённых пар, ни совсем безнадёжных
        var man = Person(1, Gender.Male, 25, Trait.Brave, Trait.Hardworking, Trait.Prudent);
        var soulmate = Person(2, Gender.Female, 25, Trait.Brave, Trait.Hardworking, Trait.Prudent);

        MakeFriends(man, soulmate);

        for (var i = 0; i < 10; i++)
        {
            var mutual = Person(20 + i, Gender.Male);
            MakeFriends(man, mutual);
            MakeFriends(soulmate, mutual);
        }

        var opposite = Person(3, Gender.Female, age: 99);

        Assert.True(MarriageSystem.GetAffinity(man, soulmate) <= 2.5);
        Assert.True(MarriageSystem.GetAffinity(man, opposite) >= 0.1);
    }

    [Fact]
    public void GetAffinity_IsMutual()
    {
        // Склонность — свойство пары, а не одного из двоих
        var man = Person(1, Gender.Male, 30, Trait.Brave);
        var woman = Person(2, Gender.Female, 40, Trait.Brave);

        Assert.Equal(MarriageSystem.GetAffinity(man, woman), MarriageSystem.GetAffinity(woman, man));
    }

    [Fact]
    public void Process_PicksTheMoreCongenialPartner()
    {
        // Оба варианта доступны, разница только в склонности
        var world = new World { CurrentYear = 10 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };

        var man = Person(1, Gender.Male, 25, Trait.Brave);
        var congenial = Person(2, Gender.Female, 25, Trait.Brave);
        var distant = Person(3, Gender.Female, 44);

        foreach (var person in new[] { man, congenial, distant })
        {
            person.Settlement = settlement;
            settlement.Members.Add(person);
            world.Characters.Add(person);
        }

        world.Settlements.Add(settlement);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 50 && man.CurrentFamily == null; year++)
        {
            world.CurrentYear = 10 + year;
            MarriageSystem.Process(world);
        }

        Assert.NotNull(man.CurrentFamily);
        Assert.Equal(congenial, man.CurrentFamily!.Mother);
    }

    [Fact]
    public void DivorceProcess_CongenialCouple_HoldsTogetherLonger()
    {
        var apart = CountDivorces(congenial: false);
        var together = CountDivorces(congenial: true);

        Assert.True(apart > together, $"союз по сердцу должен рваться реже: {together} против {apart}");
    }

    private static int CountDivorces(bool congenial)
    {
        var world = new World { CurrentYear = 100 };
        world.Settings.DivorceChance = 0.5; // Крупным шагом, чтобы разница была видна сразу

        for (var i = 0; i < 300; i++)
        {
            var husband = congenial
                ? Person(i * 2 + 1, Gender.Male, 30, Trait.Brave, Trait.Hardworking)
                : Person(i * 2 + 1, Gender.Male, 20);

            var wife = congenial
                ? Person(i * 2 + 2, Gender.Female, 30, Trait.Brave, Trait.Hardworking)
                : Person(i * 2 + 2, Gender.Female, 45);

            if (congenial)
            {
                MakeFriends(husband, wife);
            }

            var family = new Family { Id = i + 1, Father = husband, Mother = wife, FormedYear = 0 };
            husband.CurrentFamily = family;
            wife.CurrentFamily = family;

            world.Characters.Add(husband);
            world.Characters.Add(wife);
            world.Families.Add(family);
        }

        Rng.Initialize(seed: 1);
        DivorceSystem.Process(world);

        return world.Families.Count(f => f.Father.CurrentFamily == null);
    }
}
