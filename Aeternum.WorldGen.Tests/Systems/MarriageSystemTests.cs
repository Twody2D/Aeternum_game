using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

public class MarriageSystemTests
{
    [Fact]
    public void Process_NeverAssignsSamePersonToTwoFamiliesInOneYear()
    {
        var world = new World { Settings = new WorldSettings() };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        world.Settlements.Add(settlement);

        // Достаточно большой пул одиноких взрослых, чтобы за один вызов
        // почти наверняка случилось несколько браков (проверяем структурный
        // инвариант, а не конкретное их число)
        for (var i = 0; i < 15; i++)
        {
            world.Characters.Add(new Character
            {
                Id = i * 2 + 1,
                Name = $"Жених{i}",
                LastName = "Тестов",
                Gender = Gender.Male,
                Age = 25,
                Alive = true,
                Settlement = settlement
            });

            world.Characters.Add(new Character
            {
                Id = i * 2 + 2,
                Name = $"Невеста{i}",
                LastName = "Тестова",
                Gender = Gender.Female,
                Age = 22,
                Alive = true,
                Settlement = settlement
            });
        }

        MarriageSystem.Process(world);

        var marriedIds = world.Families
            .SelectMany(f => new[] { f.Father.Id, f.Mother.Id })
            .ToList();

        Assert.Equal(marriedIds.Count, marriedIds.Distinct().Count());
    }
}
