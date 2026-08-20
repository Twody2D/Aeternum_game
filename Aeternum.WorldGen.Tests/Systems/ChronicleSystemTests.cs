using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Летопись одной короны — та же сводка по периодам, что и мировая хроника,
// только отфильтрованная по WorldEvent.Kingdoms. Проверяется само сужение
// (личные события в неё не попадают, чужие войны — тоже), общие события
// многосторонних происшествий (война, союз) и то, что павшее государство
// не теряет свою историю
public class ChronicleSystemTests
{
    private static Kingdom BuildKingdom(int id, string name)
    {
        var ruler = new Character { Id = id * 100, Name = $"Правитель{id}", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult };

        return new Kingdom
        {
            Id = id,
            Name = name,
            Dynasty = new Dynasty { Id = id, Name = $"Дом{id}", FoundedYear = 1, Founder = ruler },
            Ruler = ruler,
            FoundedYear = 1
        };
    }

    [Fact]
    public void BuildChronicle_WithoutKingdom_CountsEveryEvent()
    {
        var world = new World();
        world.Events.Add(new WorldEvent { Year = 1, Type = EventType.Birth, Description = "..." });
        world.Events.Add(new WorldEvent { Year = 2, Type = EventType.Death, Description = "..." });

        var periods = ChronicleSystem.BuildChronicle(world, periodLength: 10);

        var period = Assert.Single(periods);
        Assert.Equal(2, period.Tallies.Sum(t => t.Count));
    }

    [Fact]
    public void BuildChronicle_ForKingdom_ExcludesPersonalEvents()
    {
        var world = new World();
        var kingdom = BuildKingdom(1, "Королевство Тестов");
        world.Kingdoms.Add(kingdom);

        // Личное событие вообще ни о каком государстве — Kingdoms остаётся пустым
        world.Events.Add(new WorldEvent { Year = 1, Type = EventType.Birth, Description = "..." });

        var periods = ChronicleSystem.BuildChronicle(world, periodLength: 10, kingdom: kingdom);

        Assert.Empty(periods);
    }

    [Fact]
    public void BuildChronicle_ForKingdom_ExcludesEventsOfOtherKingdoms()
    {
        var world = new World();
        var own = BuildKingdom(1, "Своё");
        var foreignKingdom = BuildKingdom(2, "Чужое");
        world.Kingdoms.Add(own);
        world.Kingdoms.Add(foreignKingdom);

        world.Events.Add(new WorldEvent
        {
            Year = 1, Type = EventType.CreationOfKingdom, Description = "...",
            Kingdoms = [foreignKingdom]
        });

        var periods = ChronicleSystem.BuildChronicle(world, periodLength: 10, kingdom: own);

        Assert.Empty(periods);
    }

    [Fact]
    public void BuildChronicle_ForKingdom_IncludesSharedEvents()
    {
        // Война — событие сразу двух претендентов; letopis каждой стороны должна его увидеть
        var world = new World();
        var a = BuildKingdom(1, "А");
        var b = BuildKingdom(2, "Б");
        world.Kingdoms.Add(a);
        world.Kingdoms.Add(b);

        world.Events.Add(new WorldEvent
        {
            Year = 5, Type = EventType.War, Description = "...",
            Kingdoms = [a, b]
        });

        var forA = ChronicleSystem.BuildChronicle(world, periodLength: 10, kingdom: a);
        var forB = ChronicleSystem.BuildChronicle(world, periodLength: 10, kingdom: b);

        Assert.Equal(1, Assert.Single(forA).Tallies.Single(t => t.Type == EventType.War).Count);
        Assert.Equal(1, Assert.Single(forB).Tallies.Single(t => t.Type == EventType.War).Count);
    }

    [Fact]
    public void BuildChronicle_FallenKingdom_StillHasItsHistory()
    {
        var world = new World();
        var kingdom = BuildKingdom(1, "Павшее");
        kingdom.FallenYear = 50;
        world.Kingdoms.Add(kingdom);

        world.Events.Add(new WorldEvent
        {
            Year = 10, Type = EventType.CreationOfKingdom, Description = "...",
            Kingdoms = [kingdom]
        });
        world.Events.Add(new WorldEvent
        {
            Year = 50, Type = EventType.FallOfKingdom, Description = "...",
            Kingdoms = [kingdom]
        });

        var periods = ChronicleSystem.BuildChronicle(world, periodLength: 10, kingdom: kingdom);

        Assert.Equal(2, periods.Sum(p => p.Tallies.Sum(t => t.Count)));
    }
}
