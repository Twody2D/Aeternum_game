using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Незаконнорождённость нигде не хранится флагом — она означает ровно одно:
// у ребёнка нет семьи рождения. Проверяется и это определение, и то, что
// из него следует в остальном мире
public class BastardSystemTests
{
    private static (World World, Settlement Settlement) BuildWorld()
    {
        var world = new World { CurrentYear = 100, AliveCount = 0 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        world.Settlements.Add(settlement);

        return (world, settlement);
    }

    private static Character Add(World world, Settlement? settlement, Gender gender, int age = 30)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 1,
            Name = $"Житель{world.Characters.Count}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Gender = gender,
            Settlement = settlement,
            Profession = "Фермер",
            BirthYear = 70
        };

        settlement?.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    private static List<Character> RunUntilBirth(World world, int years = 500)
    {
        var newborns = new List<Character>();

        Rng.Initialize(seed: 1);

        for (var year = 0; year < years && newborns.Count == 0; year++)
        {
            world.CurrentYear = 100 + year;
            BastardSystem.Process(newborns, world);
        }

        return newborns;
    }

    [Fact]
    public void Process_UnwedMother_BearsChildWithoutBirthFamily()
    {
        var (world, settlement) = BuildWorld();
        Add(world, settlement, Gender.Female);
        Add(world, settlement, Gender.Male);

        var newborns = RunUntilBirth(world);

        Assert.NotEmpty(newborns);
        Assert.Null(newborns[0].BirthFamily);
        Assert.True(BastardSystem.IsBastard(newborns[0]));
    }

    [Fact]
    public void Process_Child_CarriesMothersName()
    {
        // Отцовской семьи у него нет, значит и фамилия материнская
        var (world, settlement) = BuildWorld();
        var mother = Add(world, settlement, Gender.Female);
        mother.LastName = "Материн";

        var father = Add(world, settlement, Gender.Male);
        father.LastName = "Отцов";

        var newborns = RunUntilBirth(world);

        Assert.NotEmpty(newborns);
        Assert.Equal("Материн", newborns[0].LastName);
        Assert.Equal(mother, newborns[0].Mother);
        Assert.Equal(father, newborns[0].Father);
    }

    [Fact]
    public void Process_Child_JoinsMothersHouse()
    {
        var (world, settlement) = BuildWorld();
        var mother = Add(world, settlement, Gender.Female);
        Add(world, settlement, Gender.Male);

        var house = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = mother, FoundedYear = 1 };
        mother.Dynasty = house;
        house.Members.Add(mother);

        var newborns = RunUntilBirth(world);

        Assert.NotEmpty(newborns);
        Assert.Equal(house, newborns[0].Dynasty);
    }

    [Fact]
    public void Process_MarriedWoman_IsNotHandledHere()
    {
        // Её дети рождаются обычным порядком (см. BirthSystem)
        var (world, settlement) = BuildWorld();
        var wife = Add(world, settlement, Gender.Female);
        var husband = Add(world, settlement, Gender.Male);

        var family = new Family { Id = 1, Father = husband, Mother = wife, FormedYear = 90 };
        wife.CurrentFamily = family;
        husband.CurrentFamily = family;
        world.Families.Add(family);

        var newborns = RunUntilBirth(world, years: 200);

        Assert.Empty(newborns);
    }

    [Fact]
    public void Process_NoEligibleMan_MeansNoChild()
    {
        var (world, settlement) = BuildWorld();
        Add(world, settlement, Gender.Female);

        var newborns = RunUntilBirth(world, years: 200);

        Assert.Empty(newborns);
    }

    [Fact]
    public void Process_FatherIsNeverCloseKin()
    {
        // Единственный мужчина в поселении — родной брат: ребёнка быть не должно
        var (world, settlement) = BuildWorld();
        var parent = Add(world, settlement, Gender.Female, age: 60);

        var sister = Add(world, settlement, Gender.Female);
        var brother = Add(world, settlement, Gender.Male);

        sister.Mother = parent;
        brother.Mother = parent;

        var newborns = RunUntilBirth(world, years: 200);

        Assert.DoesNotContain(newborns, n => n.Mother == sister);
    }

    [Fact]
    public void Process_EnemyIsNeverTheFather()
    {
        var (world, settlement) = BuildWorld();
        var mother = Add(world, settlement, Gender.Female);
        var foe = Add(world, settlement, Gender.Male);

        mother.Enemies.Add(foe);
        foe.Enemies.Add(mother);

        var newborns = RunUntilBirth(world, years: 200);

        Assert.Empty(newborns);
    }

    [Fact]
    public void IsBastard_LegitimateChildAndRootlessAdult_AreBothFalse()
    {
        // Стартовые жители мира родителей не имеют вовсе — и незаконнорождёнными
        // от этого не становятся
        var (world, settlement) = BuildWorld();
        var mother = Add(world, settlement, Gender.Female);
        var father = Add(world, settlement, Gender.Male);
        var rootless = Add(world, settlement, Gender.Male);

        var child = Add(world, settlement, Gender.Male, age: 5);
        var family = new Family { Id = 1, Father = father, Mother = mother, FormedYear = 90 };

        child.Mother = mother;
        child.Father = father;
        child.BirthFamily = family;
        family.Children.Add(child);

        Assert.False(BastardSystem.IsBastard(child));
        Assert.False(BastardSystem.IsBastard(rootless));
    }

    [Fact]
    public void PickHeir_LegitimateHeirOutranksBastard()
    {
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", BirthYear = 10, Alive = true };
        var culture = new Culture { Id = 1, Name = "Тестовый народ", SuccessionLaw = SuccessionLaw.Seniority };
        ruler.Settlement = new Settlement { Id = 1, Name = "Тестовка", Culture = culture };

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Ruler = ruler,
            Dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 }
        };

        // Незаконнорождённый старше — по обычаю старшинства он бы и наследовал
        var bastard = new Character { Id = 2, Name = "Бастард", LastName = "Тестов", BirthYear = 5, Alive = true, Mother = ruler };
        var legitimate = new Character { Id = 3, Name = "Законный", LastName = "Тестов", BirthYear = 40, Alive = true };

        var family = new Family { Id = 1, Mother = ruler, FormedYear = 30 };
        legitimate.Mother = ruler;
        legitimate.BirthFamily = family;
        family.Children.Add(legitimate);

        Assert.Equal(legitimate, SuccessionSystem.PickHeir([bastard, legitimate], kingdom, ruler));
    }

    [Fact]
    public void PickHeir_WithoutLegitimateKin_TheBastardInherits()
    {
        // Иначе дом с одним лишь незаконнорождённым потомком просто угас бы
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", BirthYear = 10, Alive = true };
        var culture = new Culture { Id = 1, Name = "Тестовый народ", SuccessionLaw = SuccessionLaw.Seniority };
        ruler.Settlement = new Settlement { Id = 1, Name = "Тестовка", Culture = culture };

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Ruler = ruler,
            Dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 }
        };

        var bastard = new Character { Id = 2, Name = "Бастард", LastName = "Тестов", BirthYear = 5, Alive = true, Mother = ruler };

        Assert.Equal(bastard, SuccessionSystem.PickHeir([bastard], kingdom, ruler));
    }
}
