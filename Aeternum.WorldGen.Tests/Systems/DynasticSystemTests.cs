using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Родство домов нигде не хранится: оно выводится из живых браков между
// знатью. Проверяется само определение — включая случаи, когда родства быть
// не должно, — и обе политические его стороны
public class DynasticSystemTests
{
    private static World BuildTwoRealms(out Kingdom first, out Kingdom second)
    {
        var world = new World { CurrentYear = 100 };

        first = AddRealm(world, id: 1, x: 0);
        second = AddRealm(world, id: 2, x: 300);

        return world;
    }

    private static Kingdom AddRealm(World world, int id, double x)
    {
        var seat = new Settlement { Id = id, Name = $"Столица{id}", X = x, Y = 0 };
        var ruler = AddPerson(world, seat, Gender.Male);

        var dynasty = new Dynasty { Id = id, Name = $"Дом{id}", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);
        ruler.Dynasty = dynasty;

        var kingdom = new Kingdom
        {
            Id = id,
            Name = $"Королевство{id}",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat
        };

        world.Settlements.Add(seat);
        world.Kingdoms.Add(kingdom);

        return kingdom;
    }

    private static Character AddPerson(World world, Settlement settlement, Gender gender)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 1,
            Name = $"Особа{world.Characters.Count}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Gender = gender,
            Settlement = settlement,
            Profession = "Фермер"
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    // Брак между двумя домами; noble определяет, будет ли он политическим
    private static Family Wed(World world, Kingdom groomRealm, Kingdom brideRealm, bool noble)
    {
        var groom = noble ? groomRealm.Ruler : AddPerson(world, groomRealm.Settlements[0], Gender.Male);
        var bride = AddPerson(world, brideRealm.Settlements[0], Gender.Female);

        bride.Dynasty = brideRealm.Dynasty;
        brideRealm.Dynasty.Members.Add(bride);

        if (!noble)
        {
            groom.Dynasty = groomRealm.Dynasty;
            groomRealm.Dynasty.Members.Add(groom);
        }
        else
        {
            // Невеста знатна как жена государя: знатность даёт служба короне
            // и одно поколение после неё (см. EstateSystem)
            brideRealm.Court[CourtOffice.Heir] = bride;
        }

        var family = new Family { Id = world.Families.Count + 1, Father = groom, Mother = bride, FormedYear = 90 };

        groom.CurrentFamily = family;
        bride.CurrentFamily = family;
        world.Families.Add(family);

        return family;
    }

    [Fact]
    public void AreHousesWed_WithoutMarriage_IsFalse()
    {
        var world = BuildTwoRealms(out var first, out var second);

        Assert.False(DynasticSystem.AreRealmsWed(first, second, world));
    }

    [Fact]
    public void AreHousesWed_NobleMarriage_TiesTheHouses()
    {
        var world = BuildTwoRealms(out var first, out var second);
        Wed(world, first, second, noble: true);

        Assert.True(DynasticSystem.AreRealmsWed(first, second, world));
    }

    [Fact]
    public void AreHousesWed_CommonersOfTheSameHouses_TieNothing()
    {
        // Дома разрастаются на сотни человек: считать родством любой брак между
        // их членами значило бы породнить почти всех со всеми
        var world = BuildTwoRealms(out var first, out var second);
        Wed(world, first, second, noble: false);

        Assert.False(DynasticSystem.AreRealmsWed(first, second, world));
    }

    [Fact]
    public void AreHousesWed_WidowedTie_Lapses()
    {
        // Связь держится, пока живы оба супруга, и не поддержана — истекает
        var world = BuildTwoRealms(out var first, out var second);
        var match = Wed(world, first, second, noble: true);

        Assert.True(DynasticSystem.AreRealmsWed(first, second, world));

        match.Mother.Alive = false;

        Assert.False(DynasticSystem.AreRealmsWed(first, second, world));
    }

    [Fact]
    public void AreHousesWed_SameHouse_IsNeverKinship()
    {
        var world = BuildTwoRealms(out var first, out _);

        Assert.False(DynasticSystem.AreHousesWed(first.Dynasty, first.Dynasty, world));
    }

    [Fact]
    public void GetAllianceFactor_KinshipHelpsToAgree()
    {
        var world = BuildTwoRealms(out var first, out var second);

        var apart = DynasticSystem.GetAllianceFactor(first, second, world);

        Wed(world, first, second, noble: true);

        Assert.True(DynasticSystem.GetAllianceFactor(first, second, world) > apart);
    }

    [Fact]
    public void GetWarRestraint_KinshipHoldsSwordsBack()
    {
        var world = BuildTwoRealms(out var first, out var second);
        var claimants = new List<Kingdom> { first, second };

        var strangers = DynasticSystem.GetWarRestraint(claimants, world);

        Wed(world, first, second, noble: true);

        Assert.True(DynasticSystem.GetWarRestraint(claimants, world) < strangers);
    }

    [Fact]
    public void GetMatchAffinity_OnlyCountsHeirsOfDifferentThrones()
    {
        var world = BuildTwoRealms(out var first, out var second);

        var groom = first.Ruler;
        var foreignBride = AddPerson(world, second.Settlements[0], Gender.Female);
        foreignBride.Dynasty = second.Dynasty;

        var ownBride = AddPerson(world, first.Settlements[0], Gender.Female);
        ownBride.Dynasty = first.Dynasty;

        var commoner = AddPerson(world, first.Settlements[0], Gender.Female);

        Assert.True(DynasticSystem.GetMatchAffinity(groom, foreignBride, world) > 0);
        Assert.Equal(0, DynasticSystem.GetMatchAffinity(groom, ownBride, world));
        Assert.Equal(0, DynasticSystem.GetMatchAffinity(groom, commoner, world));
    }

    [Fact]
    public void GetAffinity_DynasticMatchIsMoreAttractive()
    {
        // Та же надбавка, но проверенная там, где она применяется на деле
        var world = BuildTwoRealms(out var first, out var second);

        var groom = first.Ruler;
        var foreignBride = AddPerson(world, second.Settlements[0], Gender.Female);
        foreignBride.Dynasty = second.Dynasty;
        second.Court[CourtOffice.Heir] = foreignBride;

        var localNoble = AddPerson(world, first.Settlements[0], Gender.Female);
        first.Court[CourtOffice.Treasurer] = localNoble;

        Assert.True(MarriageSystem.GetAffinity(groom, foreignBride, world)
                    > MarriageSystem.GetAffinity(groom, localNoble, world));
    }

    [Fact]
    public void AllianceProcess_KinRealmsAllyMoreOften()
    {
        var strangers = CountAlliances(kin: false);
        var kin = CountAlliances(kin: true);

        Assert.True(kin > strangers, $"породнившиеся дома должны сговариваться чаще: {kin} против {strangers}");
    }

    [Fact]
    public void WarProcess_KinRealmsComeToBlowsLessOften()
    {
        // Проверяется не сама шкала сдержанности, а то, что война её слушает
        var strangers = CountWars(kin: false);
        var kin = CountWars(kin: true);

        Assert.True(kin < strangers, $"спор родни должен доходить до войны реже: {kin} против {strangers}");
    }

    private static int CountWars(bool kin)
    {
        var wars = 0;

        for (var run = 0; run < 100; run++)
        {
            var world = BuildTwoRealms(out var first, out var second);

            // Спорная земля: оба государства считают её своей
            var disputed = new Settlement { Id = 99, Name = "Спорная", X = 150, Y = 0 };
            AddPerson(world, disputed, Gender.Male);

            world.Settlements.Add(disputed);
            first.Settlements.Add(disputed);
            second.Settlements.Add(disputed);

            if (kin)
            {
                Wed(world, first, second, noble: true);
            }

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            if (disputed.SiegeYears > 0)
            {
                wars++;
            }
        }

        return wars;
    }

    private static int CountAlliances(bool kin)
    {
        var allied = 0;

        for (var run = 0; run < 200; run++)
        {
            var world = BuildTwoRealms(out var first, out var second);

            // Союз завязывается на общей вере — она нужна в обоих случаях
            var faith = new Religion { Id = 1, Name = "Общая вера" };
            first.Settlements[0].Religion = faith;
            second.Settlements[0].Religion = faith;

            if (kin)
            {
                Wed(world, first, second, noble: true);
            }

            Rng.Initialize(seed: run + 1);
            AllianceSystem.Process(world);

            if (first.AlliedKingdoms.Contains(second))
            {
                allied++;
            }
        }

        return allied;
    }
}
