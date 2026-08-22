using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Стабильность трона при переходе не к прямому потомку (см. TryTriggerSuccessionCrisis)
// теперь слушает и нрав самого нового правителя — та же пара черт, что уже
// решает налоги, войну и союз, здесь работает напрямую на стабильность
public class KingdomSystemTests
{
    [Fact]
    public void Process_HardworkingHeir_TriggersFewerCivilWarsThanFrailHeir()
    {
        var hardworking = CountCivilWars(Trait.Hardworking);
        var frail = CountCivilWars(Trait.Frail);

        Assert.True(hardworking < frail,
            $"усердный наследник должен реже доводить до распри, чем хворый: {hardworking} против {frail}");
    }

    private static int CountCivilWars(Trait heirTrait)
    {
        var wars = 0;

        for (var run = 0; run < 300; run++)
        {
            var world = new World { CurrentYear = 100 };

            var previousRuler = new Character
            {
                Id = 1, Name = "Прежний", LastName = "Тестов", Age = 70,
                Alive = false, LifeStage = LifeStage.Elder
            };

            // Не связан с прежним правителем по родителям — переход трона
            // гарантированно не будет прямым наследованием (isDirectHeir = false)
            var heir = new Character
            {
                Id = 2, Name = "Наследник", LastName = "Тестов", Age = 40,
                Alive = true, LifeStage = LifeStage.Adult
            };
            heir.Traits.Add(heirTrait);

            // Второй живой родич — иначе кризис некому оспорить: соперничать
            // должно быть с кем (см. MurderSystem, тот же принцип)
            var rival = new Character
            {
                Id = 3, Name = "Родич", LastName = "Тестов", Age = 35,
                Alive = true, LifeStage = LifeStage.Adult
            };

            var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = previousRuler };
            dynasty.Members.AddRange([previousRuler, heir, rival]);

            var kingdom = new Kingdom
            {
                Id = 1,
                Name = "Королевство Тестов",
                Dynasty = dynasty,
                Ruler = previousRuler,
                FoundedYear = 1
            };

            world.Kingdoms.Add(kingdom);

            Rng.Initialize(seed: run + 1);
            KingdomSystem.Process(world);

            if (world.Events.Any(e => e.Type == EventType.CivilWar))
            {
                wars++;
            }
        }

        return wars;
    }

    [Fact]
    public void Process_MinorHeir_TriggersMoreCivilWarsThanAdultHeir()
    {
        var minor = CountCivilWarsByHeirAge(LifeStage.Child, age: 6);
        var adult = CountCivilWarsByHeirAge(LifeStage.Adult, age: 40);

        Assert.True(minor > adult,
            $"малолетний наследник должен доводить до распри чаще взрослого: {minor} против {adult}");
    }

    private static int CountCivilWarsByHeirAge(LifeStage heirStage, int age)
    {
        var wars = 0;

        for (var run = 0; run < 300; run++)
        {
            var world = new World { CurrentYear = 100 };

            var previousRuler = new Character
            {
                Id = 1, Name = "Прежний", LastName = "Тестов", Age = 70,
                Alive = false, LifeStage = LifeStage.Elder
            };

            var heir = new Character
            {
                Id = 2, Name = "Наследник", LastName = "Тестов", Age = age,
                Alive = true, LifeStage = heirStage
            };

            var rival = new Character
            {
                Id = 3, Name = "Родич", LastName = "Тестов", Age = 35,
                Alive = true, LifeStage = LifeStage.Adult
            };

            var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = previousRuler };
            dynasty.Members.AddRange([previousRuler, heir, rival]);

            var kingdom = new Kingdom
            {
                Id = 1,
                Name = "Королевство Тестов",
                Dynasty = dynasty,
                Ruler = previousRuler,
                FoundedYear = 1
            };

            world.Kingdoms.Add(kingdom);

            Rng.Initialize(seed: run + 1);
            KingdomSystem.Process(world);

            if (world.Events.Any(e => e.Type == EventType.CivilWar))
            {
                wars++;
            }
        }

        return wars;
    }

    [Fact]
    public void GetRegent_AdultRuler_ReturnsNull()
    {
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult };
        var kingdom = new Kingdom
        {
            Id = 1, Name = "Королевство", FoundedYear = 1, Ruler = ruler,
            Dynasty = new Dynasty { Id = 1, Name = "Дом", FoundedYear = 1, Founder = ruler }
        };

        Assert.Null(KingdomSystem.GetRegent(kingdom, new World()));
    }

    [Fact]
    public void GetRegent_MinorRulerWithAdultKin_ReturnsBestTrustedRelative()
    {
        var ruler = new Character { Id = 1, Name = "Дитя", LastName = "Тестов", Age = 6, Alive = true, LifeStage = LifeStage.Child };

        var trusted = new Character { Id = 2, Name = "Верный", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult, BirthYear = 60 };
        var enemyLaden = new Character { Id = 3, Name = "Спорный", LastName = "Тестов", Age = 45, Alive = true, LifeStage = LifeStage.Adult, BirthYear = 55 };

        var stranger = new Character { Id = 4, Name = "Чужой", LastName = "Иноземцев", Age = 30, Alive = true, LifeStage = LifeStage.Adult };
        enemyLaden.Enemies.Add(stranger);
        stranger.Enemies.Add(enemyLaden);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = ruler };
        dynasty.Members.AddRange([ruler, trusted, enemyLaden]);

        var kingdom = new Kingdom { Id = 1, Name = "Королевство", FoundedYear = 1, Ruler = ruler, Dynasty = dynasty };

        var regent = KingdomSystem.GetRegent(kingdom, new World());

        Assert.Equal(trusted, regent);
    }

    [Fact]
    public void GetRegent_MinorRulerWithoutKin_FallsBackToBestCourtier()
    {
        var ruler = new Character { Id = 1, Name = "Дитя", LastName = "Тестов", Age = 6, Alive = true, LifeStage = LifeStage.Child };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = ruler };
        dynasty.Members.Add(ruler);

        var chancellor = new Character { Id = 2, Name = "Советник", LastName = "Тестов", Age = 50, Alive = true, LifeStage = LifeStage.Adult };

        var kingdom = new Kingdom { Id = 1, Name = "Королевство", FoundedYear = 1, Ruler = ruler, Dynasty = dynasty };
        kingdom.Court[CourtOffice.Chancellor] = chancellor;

        var regent = KingdomSystem.GetRegent(kingdom, new World());

        Assert.Equal(chancellor, regent);
    }

    [Fact]
    public void GetRegent_MinorRulerWithoutKinOrCourt_ReturnsNull()
    {
        var ruler = new Character { Id = 1, Name = "Дитя", LastName = "Тестов", Age = 6, Alive = true, LifeStage = LifeStage.Child };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = ruler };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom { Id = 1, Name = "Королевство", FoundedYear = 1, Ruler = ruler, Dynasty = dynasty };

        Assert.Null(KingdomSystem.GetRegent(kingdom, new World()));
    }
}
