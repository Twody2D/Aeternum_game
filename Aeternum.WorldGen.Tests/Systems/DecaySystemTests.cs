using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Ветшание завязано на бросок кости, но два его края полностью детерминированы,
// и именно в них живёт смысл правила: нужное поселению не ветшает никогда,
// а лишнее при неизбежном броске ветшает всегда
public class DecaySystemTests
{
    private static World WorldWithDecayChance(double chance)
    {
        var world = new World();
        world.Settings.BuildingDecayChance = chance;

        return world;
    }

    [Fact]
    public void ShouldDecay_ExactlyEnoughBuildings_NeverDecays()
    {
        // Даже при гарантированном броске: то, чем пользуются, чинят
        var world = WorldWithDecayChance(1.0);

        Assert.False(DecaySystem.ShouldDecay(current: 5, target: 5, world));
    }

    [Fact]
    public void ShouldDecay_FewerThanNeeded_NeverDecays()
    {
        var world = WorldWithDecayChance(1.0);

        Assert.False(DecaySystem.ShouldDecay(current: 2, target: 5, world));
    }

    [Fact]
    public void ShouldDecay_SurplusWithCertainRoll_Decays()
    {
        var world = WorldWithDecayChance(1.0);

        Assert.True(DecaySystem.ShouldDecay(current: 6, target: 5, world));
    }

    [Fact]
    public void ShouldDecay_SurplusWithImpossibleRoll_Survives()
    {
        var world = WorldWithDecayChance(0.0);

        Assert.False(DecaySystem.ShouldDecay(current: 6, target: 5, world));
    }

    [Fact]
    public void ShouldDecay_AbandonedSettlement_LosesEverything()
    {
        // Брошенное поселение — target равен нулю, и постройки должны доветшать
        // до пустого места, а не застыть памятником
        var world = WorldWithDecayChance(1.0);

        Assert.True(DecaySystem.ShouldDecay(current: 1, target: 0, world));
        Assert.False(DecaySystem.ShouldDecay(current: 0, target: 0, world));
    }
}
