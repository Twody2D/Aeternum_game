using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Войско как единая величина: у него есть сила, считаемая умением, и цена,
// которую платит казна. Проверяется и то и другое, включая роспуск при
// пустой казне
public class ArmySystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Seat) BuildRealm(double treasury = 10_000)
    {
        var world = new World { CurrentYear = 100 };
        var seat = new Settlement { Id = 1, Name = "Столица" };

        var ruler = Add(world, seat, "Фермер");

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat,
            FoodTreasury = treasury
        };

        world.Settlements.Add(seat);
        world.Kingdoms.Add(kingdom);

        return (world, kingdom, seat);
    }

    private static Character Add(World world, Settlement settlement, string profession, int professionYear = 100)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 1,
            Name = $"Воин{world.Characters.Count}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = profession,
            ProfessionYear = professionYear
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    [Fact]
    public void Process_TreasuryPaysForTheArmy()
    {
        var (world, kingdom, seat) = BuildRealm();

        for (var i = 0; i < 5; i++)
        {
            Add(world, seat, "Воин");
        }

        ArmySystem.Process(world);

        Assert.True(kingdom.FoodTreasury < 10_000, "войско обязано стоить казне");
    }

    [Fact]
    public void Process_NoSoldiers_CostsNothing()
    {
        var (world, kingdom, _) = BuildRealm();

        ArmySystem.Process(world);

        Assert.Equal(10_000, kingdom.FoodTreasury);
    }

    [Fact]
    public void Process_EmptyTreasury_DisbandsPartOfTheArmy()
    {
        var (world, kingdom, seat) = BuildRealm(treasury: 0);

        var soldiers = new List<Character>();

        for (var i = 0; i < 8; i++)
        {
            soldiers.Add(Add(world, seat, "Воин"));
        }

        Rng.Initialize(seed: 1);
        ArmySystem.Process(world);

        var left = soldiers.Count(s => ProfessionSystem.GetCategory(s.Profession) != ProfessionCategory.Military);

        Assert.True(left > 0, "платить нечем — часть войска обязана разойтись");
        Assert.True(left < soldiers.Count, "разом расходится не всё войско");
        Assert.Contains(world.Events, e => e.Type == EventType.Desertion);
    }

    [Fact]
    public void Process_Deserters_AreTheLeastSkilled()
    {
        // Первыми уходят те, кому меньше терять
        var (world, kingdom, seat) = BuildRealm(treasury: 0);

        var veteran = Add(world, seat, "Воин", professionYear: 50);
        var recruit = Add(world, seat, "Воин", professionYear: 100);

        Rng.Initialize(seed: 1);
        ArmySystem.Process(world);

        Assert.Equal(ProfessionCategory.Military, ProfessionSystem.GetCategory(veteran.Profession));
        Assert.NotEqual(ProfessionCategory.Military, ProfessionSystem.GetCategory(recruit.Profession));
    }

    [Fact]
    public void Process_Deserter_StartsHisNewTradeFromScratch()
    {
        var (world, kingdom, seat) = BuildRealm(treasury: 0);
        var soldier = Add(world, seat, "Воин", professionYear: 50);

        Rng.Initialize(seed: 1);
        ArmySystem.Process(world);

        Assert.Equal(world.CurrentYear, soldier.ProfessionYear);
        Assert.Equal(1.0, ProfessionSystem.GetMastery(soldier, world));
    }

    [Fact]
    public void GetStrength_SkillOutweighsNumbers()
    {
        // Пятеро ветеранов против десятка новобранцев
        var veterans = BuildRealm();
        var recruits = BuildRealm();

        for (var i = 0; i < 5; i++)
        {
            Add(veterans.World, veterans.Seat, "Воин", professionYear: 50);
        }

        for (var i = 0; i < 6; i++)
        {
            Add(recruits.World, recruits.Seat, "Воин", professionYear: 100);
        }

        Assert.True(ArmySystem.GetStrength(veterans.Kingdom, veterans.World)
                    > ArmySystem.GetStrength(recruits.Kingdom, recruits.World));
    }

    [Fact]
    public void GetStrength_MarshalLeadsTheWholeArmyBetter()
    {
        var (world, kingdom, seat) = BuildRealm();

        for (var i = 0; i < 5; i++)
        {
            Add(world, seat, "Воин", professionYear: 60);
        }

        var without = ArmySystem.GetStrength(kingdom, world);

        CourtSystem.Process(world);

        Assert.True(CourtSystem.HasOffice(kingdom, CourtOffice.Marshal), "воевода должен найтись среди воинов");
        Assert.True(ArmySystem.GetStrength(kingdom, world) > without);
    }

    [Fact]
    public void GetGarrisonStrength_CountsOnlyThatTown()
    {
        var (world, kingdom, seat) = BuildRealm();

        var outpost = new Settlement { Id = 2, Name = "Застава" };
        world.Settlements.Add(outpost);
        kingdom.Settlements.Add(outpost);

        Add(world, seat, "Воин");
        Add(world, outpost, "Воин");
        Add(world, outpost, "Воин");

        Assert.True(ArmySystem.GetGarrisonStrength(outpost, world) > ArmySystem.GetGarrisonStrength(seat, world));
        Assert.Equal(0, ArmySystem.GetGarrisonStrength(new Settlement { Id = 3, Name = "Пусто" }, world));
    }

    [Fact]
    public void WarProcess_SkilledGarrisonLosesFewerPeople()
    {
        // Проверяется не сама шкала гарнизона, а то, что осада её слушает
        var veterans = CasualtiesUnderSiege(professionYear: 50);
        var recruits = CasualtiesUnderSiege(professionYear: 100);

        Assert.True(veterans < recruits, $"ветераны на стенах должны спасать больше жизней: {veterans} против {recruits}");
    }

    private static int CasualtiesUnderSiege(int professionYear)
    {
        var world = new World { CurrentYear = 100 };

        var disputed = new Settlement { Id = 1, Name = "Спорная" };
        world.Settlements.Add(disputed);

        // Мирные жители, ради которых всё и считается
        for (var i = 0; i < 60; i++)
        {
            Add(world, disputed, "Фермер");
        }

        // Гарнизон нарочно небольшой: бонус обороны упирается в потолок,
        // и на шести воинах ветераны с новобранцами дают уже одно и то же
        for (var i = 0; i < 3; i++)
        {
            Add(world, disputed, "Воин", professionYear);
        }

        // Два государства, каждое считает эту землю своей
        foreach (var id in new[] { 1, 2 })
        {
            var seat = new Settlement { Id = 10 + id, Name = $"Столица{id}" };
            var ruler = Add(world, seat, "Фермер");

            world.Settlements.Add(seat);
            world.Kingdoms.Add(new Kingdom
            {
                Id = id,
                Name = $"Королевство{id}",
                Dynasty = new Dynasty { Id = id, Name = $"Дом{id}", Founder = ruler, FoundedYear = 1 },
                Ruler = ruler,
                FoundedYear = 1,
                Settlements = [seat, disputed],
                Capital = seat
            });
        }

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 20; year++)
        {
            world.CurrentYear = 100 + year;
            WarSystem.Process(world);
        }

        return world.Characters.Count(c => !c.Alive && c.DeathReason == DeathReason.War);
    }

    [Fact]
    public void RebellionProcess_StrongerArmySuppressesMoreOften()
    {
        // Одна и та же корона, одна и та же казна — разница только в умении
        // войска. Считается доля успеха на большой выборке: по одному походу
        // разницу не увидеть, там оба справляются за пару лет
        var veterans = SuppressionsOutOf(200, professionYear: 50);
        var recruits = SuppressionsOutOf(200, professionYear: 100);

        Assert.True(veterans > recruits, $"опытное войско должно давить вернее: {veterans} против {recruits}");
    }

    private static int SuppressionsOutOf(int attempts, int professionYear)
    {
        var suppressed = 0;

        for (var run = 0; run < attempts; run++)
        {
            var (world, kingdom, seat) = BuildRealm();

            var province = new Settlement { Id = 2, Name = "Провинция" };
            world.Settlements.Add(province);
            kingdom.Settlements.Add(province);

            Add(world, province, "Фермер");

            for (var i = 0; i < 3; i++)
            {
                Add(world, seat, "Воин", professionYear);
            }

            province.RebellingAgainst = kingdom;
            province.RebellingUntilYear = world.CurrentYear + 10_000;

            Rng.Initialize(seed: run + 1);
            RebellionSystem.Process(world);

            if (!RebellionSystem.IsRebelling(province, world))
            {
                suppressed++;
            }
        }

        return suppressed;
    }
}
