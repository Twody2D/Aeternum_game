using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Изгнание вместо казни: не каждая отобранная "жертва" гибнет — часть
// покидает поселение живой, с враждой к тому, кто её покарал (см.
// KingdomSystem.TryTriggerSuccessionCrisis, RebellionSystem.Suppress)
public class ExileSystemTests
{
    private static (World World, Settlement Origin, Settlement Other, Character Victim, Character Punisher) BuildWorld()
    {
        var world = new World { CurrentYear = 100 };
        var origin = new Settlement { Id = 1, Name = "Родное" };
        var other = new Settlement { Id = 2, Name = "Чужое" };

        var victim = new Character { Id = 1, Name = "Жертва", LastName = "Тестов", Age = 30, Alive = true, LifeStage = LifeStage.Adult, Settlement = origin };
        var punisher = new Character { Id = 2, Name = "Каратель", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult, Settlement = origin };

        origin.Members.Add(victim);
        origin.Members.Add(punisher);
        world.Characters.AddRange([victim, punisher]);
        world.Settlements.AddRange([origin, other]);

        return (world, origin, other, victim, punisher);
    }

    [Fact]
    public void SplitCasualties_SomeVictimsAreSometimesExiledAliveInsteadOfKilled()
    {
        var exiledAlive = false;

        for (var run = 0; run < 300 && !exiledAlive; run++)
        {
            var (world, _, _, victim, punisher) = BuildWorld();

            Rng.Initialize(seed: run + 1);
            var toKill = ExileSystem.SplitCasualties([victim], punisher, world);

            exiledAlive = !toKill.Contains(victim) && victim.Alive;
        }

        Assert.True(exiledAlive, "хотя бы раз за 300 попыток жертва должна остаться в живых, изгнанной");
    }

    [Fact]
    public void SplitCasualties_ExiledVictim_LeavesOriginSettlementAndGainsEnmityWithPunisher()
    {
        var moved = false;

        for (var run = 0; run < 300 && !moved; run++)
        {
            var (world, origin, other, victim, punisher) = BuildWorld();

            Rng.Initialize(seed: run + 1);
            var toKill = ExileSystem.SplitCasualties([victim], punisher, world);

            if (toKill.Contains(victim))
            {
                continue; // В этой попытке жертву казнили — не тот случай, что проверяем
            }

            Assert.Equal(other, victim.Settlement);
            Assert.Contains(punisher, victim.Enemies);
            Assert.Contains(victim, punisher.Enemies);
            Assert.Contains(world.Events, e => e.Type == EventType.Exile);
            moved = true;
        }

        Assert.True(moved, "хотя бы раз за 300 попыток должен был случиться сам факт изгнания");
    }

    [Fact]
    public void SplitCasualties_KilledVictim_NeverGetsExileEnmityOrEvent()
    {
        for (var run = 0; run < 300; run++)
        {
            var (world, _, _, victim, punisher) = BuildWorld();

            Rng.Initialize(seed: run + 1);
            var toKill = ExileSystem.SplitCasualties([victim], punisher, world);

            if (!toKill.Contains(victim))
            {
                continue; // Эта попытка — изгнание, не то, что проверяем здесь
            }

            Assert.DoesNotContain(punisher, victim.Enemies);
            Assert.DoesNotContain(world.Events, e => e.Type == EventType.Exile);
        }
    }

    [Fact]
    public void SplitCasualties_EmptyList_ReturnsEmptyAndAddsNoEvents()
    {
        var (world, _, _, _, punisher) = BuildWorld();

        var result = ExileSystem.SplitCasualties([], punisher, world);

        Assert.Empty(result);
        Assert.Empty(world.Events);
    }
}
