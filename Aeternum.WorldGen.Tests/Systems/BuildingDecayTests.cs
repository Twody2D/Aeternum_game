using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Строительные системы проверяются целиком, через Process: правило "лишнее
// ветшает, нужное чинят" размазано между общим DecaySystem и каждой из них,
// и именно на стыке оно уже однажды не сработало — опустевшие поселения
// выходили из цикла раньше, чем дело доходило до ветшания
public class BuildingDecayTests
{
    private static World WorldWith(Settlement settlement, double decayChance)
    {
        var world = new World();
        world.Settings.BuildingDecayChance = decayChance;
        world.Settlements.Add(settlement);

        return world;
    }

    private static void Populate(World world, Settlement settlement, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var resident = new Character
            {
                Id = i + 1,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement
            };

            settlement.Members.Add(resident);
            world.Characters.Add(resident);
        }
    }

    [Fact]
    public void HousingProcess_AbandonedSettlement_LosesHousesUntilNothingLeft()
    {
        // Ровно тот случай, который раньше застывал навсегда: жителей нет,
        // а дома стоят
        var settlement = new Settlement { Id = 1, Name = "Пустошь", Houses = 5 };
        var world = WorldWith(settlement, decayChance: 1.0);

        for (var year = 0; year < 10; year++)
        {
            HousingSystem.Process(world);
        }

        Assert.Equal(0, settlement.Houses);
    }

    [Fact]
    public void HousingProcess_PopulatedSettlement_KeepsNeededHouses()
    {
        // Даже при гарантированном броске: то, чем пользуются, не разваливается
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Houses = 1 };
        var world = WorldWith(settlement, decayChance: 1.0);
        Populate(world, settlement, 5);

        for (var year = 0; year < 10; year++)
        {
            HousingSystem.Process(world);
        }

        Assert.True(settlement.Houses >= 1, "нужный жителям дом не должен исчезнуть");
    }

    [Fact]
    public void WallProcess_AbandonedSettlement_LosesWalls()
    {
        var settlement = new Settlement { Id = 1, Name = "Пустошь", Walls = 3 };
        var world = WorldWith(settlement, decayChance: 1.0);

        for (var year = 0; year < 10; year++)
        {
            WallSystem.Process(world);
        }

        Assert.Equal(0, settlement.Walls);
    }

    [Fact]
    public void WorkshopProcess_ExtinctCraft_LosesWorkshop()
    {
        // По одним живым ремесленникам до вымершего ремесла было не добраться:
        // его типа просто нет в перечислении, и мастерская жила вечно
        var settlement = new Settlement { Id = 1, Name = "Пустошь" };
        settlement.Workshops[MaterialType.Wood] = 2;

        var world = WorldWith(settlement, decayChance: 1.0);

        for (var year = 0; year < 10; year++)
        {
            WorkshopSystem.Process(world);
        }

        Assert.Equal(0, settlement.Workshops.GetValueOrDefault(MaterialType.Wood));
    }

    [Fact]
    public void HousingProcess_NoDecayChance_KeepsEverythingStanding()
    {
        var settlement = new Settlement { Id = 1, Name = "Пустошь", Houses = 4 };
        var world = WorldWith(settlement, decayChance: 0.0);

        for (var year = 0; year < 10; year++)
        {
            HousingSystem.Process(world);
        }

        Assert.Equal(4, settlement.Houses);
    }

    [Fact]
    public void HousingProcess_WithMaterials_BuildsUpToNeed()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        settlement.MaterialStocks[MaterialType.Wood] = 1000;
        settlement.MaterialStocks[MaterialType.Stone] = 1000;

        var world = WorldWith(settlement, decayChance: 0.0);
        Populate(world, settlement, 10);

        for (var year = 0; year < 20; year++)
        {
            HousingSystem.Process(world);
        }

        Assert.True(settlement.Houses > 0, "при материалах и жителях дома обязаны появиться");
    }
}
