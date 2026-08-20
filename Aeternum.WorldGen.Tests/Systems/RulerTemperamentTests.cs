using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Нрав государя (Trait.Brave/Prudent) уже двигает налоговую политику
// (см. TributeSystem.ChooseRate) — здесь та же черта решает войну и союз:
// смелый охотнее берётся за оружие и обходится без чужой помощи, осторожный
// наоборот. Проверяется на большой выборке — один бросок ничего не доказывает
public class RulerTemperamentTests
{
    private static Kingdom BuildRealm(int id, double x, Trait? rulerTrait = null)
    {
        var ruler = new Character
        {
            Id = id, Name = $"Правитель{id}", LastName = "Тестов", Age = 40,
            Alive = true, LifeStage = LifeStage.Adult
        };

        if (rulerTrait.HasValue)
        {
            ruler.Traits.Add(rulerTrait.Value);
        }

        var seat = new Settlement { Id = id, Name = $"Столица{id}", X = x, Y = 0 };
        seat.Members.Add(ruler);
        ruler.Settlement = seat;

        return new Kingdom
        {
            Id = id,
            Name = $"Королевство{id}",
            Dynasty = new Dynasty { Id = id, Name = $"Дом{id}", FoundedYear = 1, Founder = ruler },
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat
        };
    }

    [Fact]
    public void WarProcess_BraveRulersFightMoreOftenThanPrudentOnes()
    {
        var brave = CountWars(Trait.Brave);
        var prudent = CountWars(Trait.Prudent);

        Assert.True(brave > prudent, $"смелые государи должны браться за оружие чаще осторожных: {brave} против {prudent}");
    }

    private static int CountWars(Trait rulerTrait)
    {
        var wars = 0;

        for (var run = 0; run < 200; run++)
        {
            var world = new World();
            var first = BuildRealm(1, x: 0, rulerTrait);
            var second = BuildRealm(2, x: 300, rulerTrait);
            world.Kingdoms.Add(first);
            world.Kingdoms.Add(second);

            var disputed = new Settlement { Id = 99, Name = "Спорная", X = 150, Y = 0 };
            disputed.Members.Add(new Character { Id = 100, Name = "Житель", LastName = "Тестов", Age = 30, Alive = true, LifeStage = LifeStage.Adult, Settlement = disputed });
            world.Settlements.Add(disputed);
            first.Settlements.Add(disputed);
            second.Settlements.Add(disputed);

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            if (disputed.SiegeYears > 0)
            {
                wars++;
            }
        }

        return wars;
    }

    [Fact]
    public void AllianceProcess_PrudentRulersAllyMoreOftenThanBraveOnes()
    {
        var prudent = CountAlliances(Trait.Prudent);
        var brave = CountAlliances(Trait.Brave);

        Assert.True(prudent > brave, $"осторожные государи должны искать союза чаще смелых: {prudent} против {brave}");
    }

    private static int CountAlliances(Trait rulerTrait)
    {
        var allied = 0;

        for (var run = 0; run < 200; run++)
        {
            var world = new World();
            var first = BuildRealm(1, x: 0, rulerTrait);
            var second = BuildRealm(2, x: 300, rulerTrait);
            world.Kingdoms.Add(first);
            world.Kingdoms.Add(second);

            // Союз завязывается на общей вере — она нужна обеим сторонам
            var faith = new Religion { Id = 1, Name = "Общая вера" };
            first.Settlements[0].Religion = faith;
            second.Settlements[0].Religion = faith;

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
