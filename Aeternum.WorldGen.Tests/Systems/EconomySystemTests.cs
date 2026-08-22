using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// EconomySystem слушает не только координаты поселения (ClimateSystem), но и
// снос климатического пояса (World.ClimateDrift, тот же ClimateSystem) — проверяется
// именно применение на Process(), а не только формула ClimateSystem.GetFertility
// саму по себе (см. ClimateSystemTests) — иначе разрыв связи мог бы пройти мимо тестов
public class EconomySystemTests
{
    private static (World World, Settlement Settlement) BuildFarm(double climateDrift)
    {
        var world = new World { CurrentYear = 100, ClimateDrift = climateDrift };
        var settlement = new Settlement { Id = 1, Name = "Тестовое", Y = ClimateSystem.MapSize / 2 };

        var farmer = new Character
        {
            Id = 1, Name = "Фермер", LastName = "Тестов", Age = 30, Alive = true,
            LifeStage = LifeStage.Adult, Settlement = settlement, Profession = "Фермер"
        };

        settlement.Members.Add(farmer);
        world.Characters.Add(farmer);
        world.Settlements.Add(settlement);

        return (world, settlement);
    }

    [Fact]
    public void Process_ClimateDrift_ChangesFoodProductionAtTheSameCoordinates()
    {
        var (worldNoDrift, noDrift) = BuildFarm(climateDrift: 0);
        var (worldMaxDrift, maxDrift) = BuildFarm(climateDrift: 200); // Пояс ушёл далеко от этого места

        EconomySystem.Process(worldNoDrift);
        EconomySystem.Process(worldMaxDrift);

        Assert.True(noDrift.FoodStock > maxDrift.FoodStock,
            "поселение в середине пояса без сноса должно собрать больше, чем оно же с поясом, ушедшим прочь");
    }
}
