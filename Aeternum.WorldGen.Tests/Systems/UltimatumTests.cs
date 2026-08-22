using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Ультиматум — дипломатия вместо бинарного война/вассалитет-после-осады:
// явный перевес сил у свежего спора может решиться бескровно, ещё до
// первого года войны. Проверяется само условие (перевес, свежесть спора)
// и то, что решение принять его зависит от нрава того, кому он выставлен
public class UltimatumTests
{
    private static int _nextId = 1;

    private static (Kingdom Kingdom, Settlement Seat) BuildRealm(World world, int population, Trait? rulerTrait = null)
    {
        var seat = new Settlement { Id = _nextId++, Name = $"Столица{_nextId}" };

        var ruler = new Character
        {
            Id = _nextId++, Name = "Правитель", LastName = "Тестов", Age = 45,
            Alive = true, LifeStage = LifeStage.Adult, Settlement = seat
        };

        if (rulerTrait.HasValue)
        {
            ruler.Traits.Add(rulerTrait.Value);
        }

        seat.Members.Add(ruler);
        world.Characters.Add(ruler);

        for (var i = 1; i < population; i++)
        {
            var resident = new Character
            {
                Id = _nextId++, Name = $"Житель{i}", LastName = "Тестов", Age = 30,
                Alive = true, LifeStage = LifeStage.Adult, Settlement = seat
            };
            seat.Members.Add(resident);
            world.Characters.Add(resident);
        }

        var kingdom = new Kingdom
        {
            Id = _nextId++,
            Name = $"Королевство{_nextId}",
            Dynasty = new Dynasty { Id = _nextId, Name = $"Дом{_nextId}", FoundedYear = 1, Founder = ruler },
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [seat],
            Capital = seat
        };

        return (kingdom, seat);
    }

    private static World BuildDispute(int strongPopulation, int weakPopulation, Trait? weakRulerTrait, out Kingdom strong, out Kingdom weak)
    {
        var world = new World { CurrentYear = 100 };

        var (strongKingdom, strongSeat) = BuildRealm(world, strongPopulation);
        var (weakKingdom, weakSeat) = BuildRealm(world, weakPopulation, weakRulerTrait);

        var disputed = new Settlement { Id = _nextId++, Name = "Спорная" };
        world.Settlements.Add(disputed);
        world.Settlements.Add(strongSeat);
        world.Settlements.Add(weakSeat);

        strongKingdom.Settlements.Add(disputed);
        weakKingdom.Settlements.Add(disputed);

        world.Kingdoms.Add(strongKingdom);
        world.Kingdoms.Add(weakKingdom);

        strong = strongKingdom;
        weak = weakKingdom;

        return world;
    }

    [Fact]
    public void Process_LopsidedFreshDispute_SometimesResolvesByUltimatumWithoutCasualties()
    {
        var resolved = false;

        for (var run = 0; run < 300 && !resolved; run++)
        {
            var world = BuildDispute(strongPopulation: 30, weakPopulation: 5, weakRulerTrait: Trait.Prudent, out var strong, out var weak);

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            if (weak.Suzerain == strong)
            {
                resolved = true;
                Assert.Contains(world.Events, e => e.Type == EventType.Vassalization && e.Description.Contains("ультиматум"));
                Assert.DoesNotContain(world.Characters, c => !c.Alive);
            }
        }

        Assert.True(resolved, "хотя бы один из 300 явно неравных споров должен был решиться ультиматумом");
    }

    [Fact]
    public void Process_EvenlyMatchedDispute_NeverGetsResolvedByUltimatum()
    {
        for (var run = 0; run < 100; run++)
        {
            var world = BuildDispute(strongPopulation: 10, weakPopulation: 9, weakRulerTrait: Trait.Prudent, out var strong, out var weak);

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            Assert.DoesNotContain(world.Events, e => e.Type == EventType.Vassalization && e.Description.Contains("ультиматум"));
        }
    }

    [Fact]
    public void Process_OngoingSiege_IsNeverInterruptedByUltimatum()
    {
        for (var run = 0; run < 100; run++)
        {
            var world = BuildDispute(strongPopulation: 30, weakPopulation: 5, weakRulerTrait: Trait.Prudent, out _, out _);
            world.Settlements.First(s => s.Name == "Спорная").SiegeYears = 1; // уже воюют не первый год

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            Assert.DoesNotContain(world.Events, e => e.Type == EventType.Vassalization && e.Description.Contains("ультиматум"));
        }
    }

    [Fact]
    public void Process_PrudentTarget_SubmitsToUltimatumMoreOftenThanBraveTarget()
    {
        var prudentSubmissions = CountSubmissions(Trait.Prudent);
        var braveSubmissions = CountSubmissions(Trait.Brave);

        Assert.True(prudentSubmissions > braveSubmissions,
            $"осторожный должен принимать ультиматум чаще смелого: {prudentSubmissions} против {braveSubmissions}");
    }

    private static int CountSubmissions(Trait weakRulerTrait)
    {
        var submissions = 0;

        for (var run = 0; run < 400; run++)
        {
            var world = BuildDispute(strongPopulation: 30, weakPopulation: 5, weakRulerTrait, out var strong, out var weak);

            Rng.Initialize(seed: run + 1);
            WarSystem.Process(world);

            if (weak.Suzerain == strong)
            {
                submissions++;
            }
        }

        return submissions;
    }
}
