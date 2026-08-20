using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Save;

namespace Aeternum.WorldGen.Tests.Save;

public class SaveSystemTests
{
    [Fact]
    public void SerializeThenDeserialize_PreservesWorldGraph()
    {
        var world = BuildWorld();

        var json = SaveSystem.Serialize(world);
        var loaded = SaveSystem.Deserialize(json);

        Assert.Equal(world.CurrentYear, loaded.CurrentYear);
        Assert.Equal(world.Characters.Count, loaded.Characters.Count);
        Assert.Equal(world.Families.Count, loaded.Families.Count);
        Assert.Equal(world.Dynasties.Count, loaded.Dynasties.Count);
        Assert.Equal(world.Settlements.Count, loaded.Settlements.Count);
        Assert.Equal(world.Events.Count, loaded.Events.Count);

        var child = Assert.Single(loaded.Characters, c => c.Id == 3);
        Assert.Equal("Ребёнок", child.Name);
        Assert.NotNull(child.Mother);
        Assert.Equal(2, child.Mother!.Id);
        Assert.NotNull(child.Father);
        Assert.Equal(1, child.Father!.Id);
        Assert.NotNull(child.BirthFamily);
        Assert.Equal(1, child.BirthFamily!.Id);
        Assert.NotNull(child.Dynasty);
        Assert.Equal("Дом Тестов", child.Dynasty!.Name);

        var settlement = Assert.Single(loaded.Settlements);
        Assert.Equal("Культ", settlement.Culture?.Name);
        Assert.Equal("Религия", settlement.Religion?.Name);

        var family = Assert.Single(loaded.Families);
        Assert.Single(family.Children, c => c.Id == 3);

        // Ссылка на государство в WorldEvent (см. WorldEvent.Kingdoms) — единственное
        // в этом событии поле-ссылка, и единственное, что не переживёт сохранение
        // молча, если сериализация забудет развернуть его в KingdomIds
        var kingdomEvent = Assert.Single(loaded.Events, e => e.Type == EventType.CreationOfKingdom);
        var loadedKingdom = Assert.Single(kingdomEvent.Kingdoms);
        Assert.Equal("Королевство Тестов", loadedKingdom.Name);
        Assert.Same(loadedKingdom, Assert.Single(loaded.Kingdoms)); // та же ссылка, что и в World.Kingdoms, не копия
    }

    private static World BuildWorld()
    {
        var culture = new Culture { Id = 1, Name = "Культ", PreferredCategory = ProfessionCategory.FoodProducer };
        var religion = new Religion { Id = 1, Name = "Религия" };
        var settlement = new Settlement { Id = 1, Name = "Село", Culture = culture, Religion = religion, FoodStock = 10 };

        var father = new Character { Id = 1, Name = "Отец", LastName = "Тестов", Gender = Gender.Male, Age = 30, Settlement = settlement };
        var mother = new Character { Id = 2, Name = "Мать", LastName = "Тестова", Gender = Gender.Female, Age = 28, Settlement = settlement };
        var child = new Character { Id = 3, Name = "Ребёнок", LastName = "Тестов", Gender = Gender.Male, Age = 0, Settlement = settlement, Mother = mother, Father = father };

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = father, FoundedYear = 1 };
        dynasty.Members.Add(father);
        dynasty.Members.Add(mother);
        dynasty.Members.Add(child);
        father.Dynasty = dynasty;
        child.Dynasty = dynasty;

        var family = new Family { Id = 1, Father = father, Mother = mother, FormedYear = 1, Dynasty = dynasty };
        family.Children.Add(child);
        child.BirthFamily = family;
        father.CurrentFamily = family;
        mother.CurrentFamily = family;
        dynasty.Families.Add(family);

        settlement.Members.Add(father);
        settlement.Members.Add(mother);
        settlement.Members.Add(child);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = father,
            FoundedYear = 1,
            Settlements = { settlement }
        };

        var world = new World
        {
            CurrentYear = 5,
            Characters = { father, mother, child },
            Families = { family },
            Dynasties = { dynasty },
            Settlements = { settlement },
            Cultures = { culture },
            Religions = { religion },
            Kingdoms = { kingdom }
        };

        world.Events.Add(new WorldEvent { Year = 1, Type = EventType.Birth, Description = "Родился Ребёнок Тестов" });
        world.Events.Add(new WorldEvent
        {
            Year = 1,
            Type = EventType.CreationOfKingdom,
            Description = "Образовано Королевство Тестов",
            Kingdoms = { kingdom }
        });

        return world;
    }
}
