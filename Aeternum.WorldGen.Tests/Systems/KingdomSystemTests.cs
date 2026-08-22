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
}
