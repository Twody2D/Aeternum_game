using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Сословие нигде не хранится: оно вычисляется по нынешнему положению человека.
// Проверяется само вычисление и оба его последствия — брачный барьер
// и неравенство перед голодом
public class EstateSystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Settlement) BuildKingdom()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Столица", X = 500, Y = 500 };

        var ruler = Person(1, "Фермер");
        ruler.Settlement = settlement;
        settlement.Members.Add(ruler);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);
        ruler.Dynasty = dynasty;

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [settlement]
        };

        world.Characters.Add(ruler);
        world.Settlements.Add(settlement);
        world.Kingdoms.Add(kingdom);

        return (world, kingdom, settlement);
    }

    private static Character Person(int id, string profession, Gender gender = Gender.Male, int age = 30)
    {
        return new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Gender = gender,
            Profession = profession,
            ProfessionYear = 90
        };
    }

    [Fact]
    public void GetEstate_RulerAndCourt_AreNobility()
    {
        var (world, kingdom, settlement) = BuildKingdom();

        var marshal = Person(2, "Воин");
        marshal.Settlement = settlement;
        settlement.Members.Add(marshal);
        world.Characters.Add(marshal);

        CourtSystem.Process(world);

        Assert.Equal(Estate.Nobility, EstateSystem.GetEstate(kingdom.Ruler, world));
        Assert.Equal(Estate.Nobility, EstateSystem.GetEstate(marshal, world));
    }

    [Fact]
    public void GetEstate_ChildOfTheCrown_IsNobleToo()
    {
        // Знатность держится одно поколение и требует подтверждения службой
        var (world, kingdom, _) = BuildKingdom();

        var child = Person(2, "Фермер");
        child.Father = kingdom.Ruler;

        var grandchild = Person(3, "Фермер");
        grandchild.Father = child;

        Assert.Equal(Estate.Nobility, EstateSystem.GetEstate(child, world));
        Assert.NotEqual(Estate.Nobility, EstateSystem.GetEstate(grandchild, world));
    }

    [Fact]
    public void GetEstate_HugeRulingHouse_DoesNotMakeEveryoneNoble()
    {
        // Правящий дом за века разрастается на весь мир: считать знатью
        // по принадлежности к нему нельзя, и это проверяется прямо
        var (world, kingdom, settlement) = BuildKingdom();

        var kin = Person(2, "Фермер");
        kin.Dynasty = kingdom.Dynasty;
        kingdom.Dynasty.Members.Add(kin);
        kin.Settlement = settlement;

        Assert.Equal(Estate.Commoners, EstateSystem.GetEstate(kin, world));
    }

    [Fact]
    public void GetEstate_TradesDivideBurghersFromCommoners()
    {
        var world = new World();

        Assert.Equal(Estate.Burghers, EstateSystem.GetEstate(Person(1, "Кузнец"), world));
        Assert.Equal(Estate.Burghers, EstateSystem.GetEstate(Person(2, "Торговец"), world));
        Assert.Equal(Estate.Burghers, EstateSystem.GetEstate(Person(3, "Учёный"), world));
        Assert.Equal(Estate.Commoners, EstateSystem.GetEstate(Person(4, "Фермер"), world));
        Assert.Equal(Estate.Commoners, EstateSystem.GetEstate(Person(5, "Воин"), world));
    }

    [Fact]
    public void GetEstate_FallenKingdom_LeavesNoNobility()
    {
        // Государства нет — некому и служить
        var (world, kingdom, _) = BuildKingdom();
        kingdom.FallenYear = world.CurrentYear;

        Assert.NotEqual(Estate.Nobility, EstateSystem.GetEstate(kingdom.Ruler, world));
    }

    [Fact]
    public void GetEstate_ChangesWithFate()
    {
        // Ремесленник, ушедший в поле от голода, перестаёт быть горожанином
        var world = new World();
        var smith = Person(1, "Кузнец");

        Assert.Equal(Estate.Burghers, EstateSystem.GetEstate(smith, world));

        smith.Profession = "Фермер";

        Assert.Equal(Estate.Commoners, EstateSystem.GetEstate(smith, world));
    }

    [Fact]
    public void GetAffinity_EqualsAreDrawnTogetherMoreThanUnequals()
    {
        var (world, kingdom, settlement) = BuildKingdom();

        var noblewoman = Person(2, "Фермер", Gender.Female);
        noblewoman.Father = kingdom.Ruler;

        var commoner = Person(3, "Фермер", Gender.Female);
        var peer = Person(4, "Фермер");
        peer.Father = kingdom.Ruler;

        foreach (var person in new[] { noblewoman, commoner, peer })
        {
            person.Settlement = settlement;
        }

        Assert.True(MarriageSystem.GetAffinity(peer, noblewoman, world)
                    > MarriageSystem.GetAffinity(peer, commoner, world),
            "через сословие переступают неохотно");
    }

    [Fact]
    public void GetStarvationShield_NobilityWeathersFamineBest()
    {
        var (world, kingdom, _) = BuildKingdom();

        var burgher = Person(2, "Кузнец");
        var commoner = Person(3, "Фермер");

        var noble = EstateSystem.GetStarvationShield(kingdom.Ruler, world);

        Assert.True(noble < EstateSystem.GetStarvationShield(burgher, world));
        Assert.True(EstateSystem.GetStarvationShield(burgher, world) < EstateSystem.GetStarvationShield(commoner, world));
        Assert.Equal(1.0, EstateSystem.GetStarvationShield(commoner, world));
    }

    [Fact]
    public void EconomyProcess_FamineTakesCommonersFirst()
    {
        // Тот же голод, те же условия — разница только в положении
        var commonersLost = CountStarved("Фермер");
        var burghersLost = CountStarved("Кузнец");

        Assert.True(commonersLost > burghersLost, $"простолюдинов голод обязан косить чаще: {commonersLost} против {burghersLost}");
    }

    private static int CountStarved(string profession)
    {
        var world = new World { CurrentYear = 10 };
        // Дефицит нарочно неподъёмный: четыре сотни работников за год сами
        // производят столько, что умеренный недород они бы просто закрыли
        var settlement = new Settlement { Id = 1, Name = "Тестовка", X = 500, Y = 500, FoodStock = -100_000 };
        world.Settlements.Add(settlement);

        for (var i = 0; i < 400; i++)
        {
            var person = Person(i + 1, profession);
            person.Settlement = settlement;
            settlement.Members.Add(person);
            world.Characters.Add(person);
        }

        Rng.Initialize(seed: 1);
        EconomySystem.Process(world);

        return world.Characters.Count(c => !c.Alive);
    }
}
