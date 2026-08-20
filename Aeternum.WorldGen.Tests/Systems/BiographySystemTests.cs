using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Жизнь персонажа рассыпана по миру: семья хранит только нынешний брак,
// опекунство — только сегодняшнего подопечного, двор — только того, кто на
// месте прямо сейчас. Проверяется, что BiographySystem действительно собирает
// прошлое обратно — включая случаи, которые фрагменты сами по себе не различают
// (умерший женат "по документам", но не по факту, см. GetStatus)
public class BiographySystemTests
{
    private static int _nextId = 1;

    private static Character Person(string name = "Житель", Gender gender = Gender.Male, int birthYear = 1)
    {
        return new Character
        {
            Id = _nextId++,
            Name = name,
            LastName = "Тестов",
            Gender = gender,
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Profession = "Фермер",
            BirthYear = birthYear
        };
    }

    private static Family Marry(World world, Character husband, Character wife, int formedYear)
    {
        var family = new Family { Id = world.Families.Count + 1, Father = husband, Mother = wife, FormedYear = formedYear };

        husband.CurrentFamily = family;
        wife.CurrentFamily = family;
        world.Families.Add(family);

        return family;
    }

    [Fact]
    public void Build_CurrentMarriage_IsReportedAsCurrent()
    {
        var world = new World();
        var husband = Person("Муж");
        var wife = Person("Жена", Gender.Female);
        world.Characters.Add(husband);
        world.Characters.Add(wife);

        Marry(world, husband, wife, formedYear: 10);

        var bio = BiographySystem.Build(husband, world);

        Assert.Single(bio.Marriages);
        Assert.Equal(MarriageStatus.Current, bio.Marriages[0].Status);
        Assert.Equal(wife, bio.Marriages[0].Spouse);
    }

    [Fact]
    public void Build_DivorcedMarriage_IsReportedAsEnded()
    {
        var world = new World();
        var husband = Person("Муж");
        var wife = Person("Жена", Gender.Female);
        world.Characters.Add(husband);
        world.Characters.Add(wife);

        Marry(world, husband, wife, formedYear: 10);
        husband.CurrentFamily = null; // Развод — оба живы, но не вместе (см. DivorceSystem)
        wife.CurrentFamily = null;

        var bio = BiographySystem.Build(husband, world);

        Assert.Equal(MarriageStatus.Ended, bio.Marriages[0].Status);
        Assert.True(bio.Marriages[0].Spouse.Alive);
    }

    [Fact]
    public void Build_WidowedMarriage_IsReportedAsEnded()
    {
        var world = new World();
        var husband = Person("Муж");
        var wife = Person("Жена", Gender.Female);
        world.Characters.Add(husband);
        world.Characters.Add(wife);

        Marry(world, husband, wife, formedYear: 10);
        wife.Alive = false; // DeathSystem.Kill освобождает только пережившего супруга — муж остаётся привязан к семье

        var bio = BiographySystem.Build(husband, world);

        Assert.Equal(MarriageStatus.Ended, bio.Marriages[0].Status);
        Assert.False(bio.Marriages[0].Spouse.Alive);
    }

    [Fact]
    public void Build_DeadCharacterInAMarriageTheSurvivorIsFreedFrom_IsNotReportedAsCurrent()
    {
        // Тот самый пойманный при разработке случай: DeathSystem.Kill освобождает
        // только пережившего супруга (spouse.CurrentFamily = null), а собственное
        // CurrentFamily умершего так и остаётся указывать на последнюю семью —
        // наивное сравнение "оба CurrentFamily совпадают" дало бы здесь неверный "развод"
        var world = new World();
        var husband = Person("Муж");
        var wife = Person("Жена", Gender.Female);
        world.Characters.Add(husband);
        world.Characters.Add(wife);

        Marry(world, husband, wife, formedYear: 10);

        husband.Alive = false; // умер первым, CurrentFamily у него не тронут
        wife.CurrentFamily = null; // а у пережившей жены — освобождён

        var bio = BiographySystem.Build(husband, world);

        Assert.Equal(MarriageStatus.Ended, bio.Marriages[0].Status);
    }

    [Fact]
    public void Build_MultipleMarriages_AreOrderedByYear()
    {
        var world = new World();
        var man = Person("Муж");
        var firstWife = Person("Первая", Gender.Female);
        var secondWife = Person("Вторая", Gender.Female);
        world.Characters.AddRange([man, firstWife, secondWife]);

        Marry(world, man, secondWife, formedYear: 40);
        Marry(world, man, firstWife, formedYear: 10); // порядок вызовов нарочно не совпадает с порядком лет

        var bio = BiographySystem.Build(man, world);

        Assert.Equal(2, bio.Marriages.Count);
        Assert.Equal(10, bio.Marriages[0].FormedYear);
        Assert.Equal(40, bio.Marriages[1].FormedYear);
    }

    [Fact]
    public void Build_CollectsChildrenAcrossAllMarriages()
    {
        var world = new World();
        var man = Person("Муж");
        var firstWife = Person("Первая", Gender.Female);
        var secondWife = Person("Вторая", Gender.Female);
        world.Characters.AddRange([man, firstWife, secondWife]);

        var firstFamily = Marry(world, man, firstWife, formedYear: 10);
        var childOne = Person("Ребёнок1", birthYear: 12);
        firstFamily.Children.Add(childOne);

        man.CurrentFamily = null;
        firstWife.CurrentFamily = null;

        var secondFamily = Marry(world, man, secondWife, formedYear: 30);
        var childTwo = Person("Ребёнок2", birthYear: 32);
        secondFamily.Children.Add(childTwo);

        var bio = BiographySystem.Build(man, world);

        Assert.Equal([childOne, childTwo], bio.Children);
    }

    [Fact]
    public void Build_FindsWardsRaisedByThisCharacter()
    {
        var world = new World();
        var guardian = Person("Опекун");
        var ward = Person("Подопечный", birthYear: 5);
        ward.Guardian = guardian;
        world.Characters.Add(guardian);
        world.Characters.Add(ward);

        var bio = BiographySystem.Build(guardian, world);

        Assert.Equal([ward], bio.Wards);
    }

    [Fact]
    public void Build_CurrentRuler_IsReported()
    {
        var world = new World();
        var ruler = Person("Правитель");
        world.Characters.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1, Name = "Королевство Тестов", FoundedYear = 1,
            Dynasty = new Dynasty { Id = 1, Name = "Дом", FoundedYear = 1, Founder = ruler },
            Ruler = ruler
        };
        world.Kingdoms.Add(kingdom);

        var bio = BiographySystem.Build(ruler, world);

        Assert.Equal(kingdom, bio.RulesKingdom);
    }

    [Fact]
    public void Build_FallenKingdomRuler_IsNotReportedAsRuling()
    {
        var world = new World();
        var ruler = Person("Бывший правитель");
        world.Characters.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1, Name = "Павшее королевство", FoundedYear = 1, FallenYear = 50,
            Dynasty = new Dynasty { Id = 1, Name = "Дом", FoundedYear = 1, Founder = ruler },
            Ruler = ruler
        };
        world.Kingdoms.Add(kingdom);

        var bio = BiographySystem.Build(ruler, world);

        Assert.Null(bio.RulesKingdom);
    }

    [Fact]
    public void Build_CourtOffice_IsReportedWithItsKingdom()
    {
        var world = new World();
        var ruler = Person("Правитель");
        var treasurer = Person("Казначей");
        world.Characters.Add(ruler);
        world.Characters.Add(treasurer);

        var kingdom = new Kingdom
        {
            Id = 1, Name = "Королевство Тестов", FoundedYear = 1,
            Dynasty = new Dynasty { Id = 1, Name = "Дом", FoundedYear = 1, Founder = ruler },
            Ruler = ruler
        };
        kingdom.Court[CourtOffice.Treasurer] = treasurer;
        world.Kingdoms.Add(kingdom);

        var bio = BiographySystem.Build(treasurer, world);

        Assert.Equal(CourtOffice.Treasurer, bio.Office);
        Assert.Equal(kingdom, bio.OfficeKingdom);
    }

    [Fact]
    public void Build_PersonWithoutOfficeOrThrone_ReportsNeither()
    {
        var world = new World();
        var commoner = Person("Простолюдин");
        world.Characters.Add(commoner);

        var bio = BiographySystem.Build(commoner, world);

        Assert.Null(bio.RulesKingdom);
        Assert.Null(bio.Office);
        Assert.Null(bio.OfficeKingdom);
    }
}
