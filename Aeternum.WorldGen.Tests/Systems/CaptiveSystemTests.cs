using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Угон пленных — второй исход войны рядом со смертью. Проверяется, кого
// уводят, куда и за кого корона платит выкуп
public class CaptiveSystemTests
{
    private static (World World, Settlement Besieged, Settlement Seat, Kingdom Captor) BuildWar()
    {
        var world = new World { CurrentYear = 100 };

        var besieged = new Settlement { Id = 1, Name = "Осаждённое" };
        var seat = new Settlement { Id = 2, Name = "Столица победителя" };

        var conqueror = new Character
        {
            Id = 1,
            Name = "Захватчик",
            LastName = "Тестов",
            Age = 40,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = seat
        };

        seat.Members.Add(conqueror);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = conqueror, FoundedYear = 1 };

        var captor = new Kingdom
        {
            Id = 1,
            Name = "Королевство Победителей",
            Dynasty = dynasty,
            Ruler = conqueror,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat // Пленных ведут в столицу (см. CapitalSystem)
        };

        world.Characters.Add(conqueror);
        world.Settlements.Add(besieged);
        world.Settlements.Add(seat);
        world.Kingdoms.Add(captor);

        return (world, besieged, seat, captor);
    }

    private static Character Add(World world, Settlement settlement, string profession = "Фермер", Gender gender = Gender.Female)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 10,
            Name = $"Житель{world.Characters.Count}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Gender = gender,
            Settlement = settlement,
            Profession = profession
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    [Fact]
    public void Process_SurvivorsAreLedAwayToTheVictorsSeat()
    {
        var (world, besieged, seat, captor) = BuildWar();

        var survivors = new List<Character>();

        for (var i = 0; i < 10; i++)
        {
            survivors.Add(Add(world, besieged));
        }

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, survivors, casualtyCount: 6, captor, world);

        Assert.Contains(survivors, s => s.Settlement == seat);
        Assert.Contains(world.Events, e => e.Type == EventType.Captivity);
    }

    [Fact]
    public void Process_NoCasualties_MeansNoCaptives()
    {
        // Уводят по итогам боя: не было боя — некого и уводить
        var (world, besieged, seat, captor) = BuildWar();

        var survivors = new List<Character> { Add(world, besieged) };

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, survivors, casualtyCount: 0, captor, world);

        Assert.All(survivors, s => Assert.Equal(besieged, s.Settlement));
        Assert.DoesNotContain(world.Events, e => e.Type == EventType.Captivity);
    }

    [Fact]
    public void Process_VictorWithoutLands_TakesNobody()
    {
        var (world, besieged, _, captor) = BuildWar();
        captor.Settlements.Clear();
        captor.Capital = null; // Земель нет — и престола тоже

        var survivors = new List<Character>();

        for (var i = 0; i < 10; i++)
        {
            survivors.Add(Add(world, besieged));
        }

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, survivors, casualtyCount: 6, captor, world);

        Assert.All(survivors, s => Assert.Equal(besieged, s.Settlement));
    }

    [Fact]
    public void Process_NobleIsRansomedAndStaysHome()
    {
        var (world, besieged, seat, captor) = BuildWar();

        // Своя корона, у которой есть чем платить
        var ruler = Add(world, besieged, "Фермер", Gender.Male);
        var homeDynasty = new Dynasty { Id = 2, Name = "Дом Осаждённых", Founder = ruler, FoundedYear = 1 };

        var home = new Kingdom
        {
            Id = 2,
            Name = "Королевство Осаждённых",
            Dynasty = homeDynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [besieged],
            GoldTreasury = 10_000
        };

        world.Kingdoms.Add(home);

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, [ruler], casualtyCount: 2, captor, world);

        Assert.Equal(besieged, ruler.Settlement);
        Assert.True(home.GoldTreasury < 10_000, "выкуп обязан стоить казне");
    }

    [Fact]
    public void Process_EmptyTreasury_LosesEvenItsNobility()
    {
        // Скупая или разорённая корона своих не выкупает
        var (world, besieged, seat, captor) = BuildWar();

        var ruler = Add(world, besieged, "Фермер", Gender.Male);
        var homeDynasty = new Dynasty { Id = 2, Name = "Дом Осаждённых", Founder = ruler, FoundedYear = 1 };

        world.Kingdoms.Add(new Kingdom
        {
            Id = 2,
            Name = "Королевство Осаждённых",
            Dynasty = homeDynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [besieged],
            GoldTreasury = 0
        });

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, [ruler], casualtyCount: 2, captor, world);

        Assert.Equal(seat, ruler.Settlement);
    }

    [Fact]
    public void Process_CommonerIsNeverRansomed()
    {
        // Корона платит за знатных, за прочих — нет, сколько бы золота у неё ни было
        var (world, besieged, seat, captor) = BuildWar();

        var ruler = Add(world, besieged, "Фермер", Gender.Male);
        var commoner = Add(world, besieged);

        var homeDynasty = new Dynasty { Id = 2, Name = "Дом Осаждённых", Founder = ruler, FoundedYear = 1 };

        var home = new Kingdom
        {
            Id = 2,
            Name = "Королевство Осаждённых",
            Dynasty = homeDynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [besieged],
            GoldTreasury = 10_000
        };

        world.Kingdoms.Add(home);

        Rng.Initialize(seed: 1);
        CaptiveSystem.Process(besieged, [commoner], casualtyCount: 2, captor, world);

        Assert.Equal(seat, commoner.Settlement);
        Assert.Equal(10_000, home.GoldTreasury);
    }

    [Fact]
    public void Process_DefendersAreTakenLessOftenThanCivilians()
    {
        // Бойцов на стенах убивают, а уводят тех, кто не отбивается
        var soldiersTaken = 0;
        var civiliansTaken = 0;

        for (var run = 0; run < 50; run++)
        {
            var (world, besieged, seat, captor) = BuildWar();

            var survivors = new List<Character>();

            for (var i = 0; i < 10; i++)
            {
                survivors.Add(Add(world, besieged, "Воин", Gender.Male));
                survivors.Add(Add(world, besieged));
            }

            Rng.Initialize(seed: run + 1);
            CaptiveSystem.Process(besieged, survivors, casualtyCount: 8, captor, world);

            soldiersTaken += survivors.Count(s => s.Settlement == seat && s.Profession == "Воин");
            civiliansTaken += survivors.Count(s => s.Settlement == seat && s.Profession == "Фермер");
        }

        Assert.True(civiliansTaken > soldiersTaken, $"уводят прежде всего невоюющих: {civiliansTaken} против {soldiersTaken}");
    }
}
