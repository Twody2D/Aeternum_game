using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Карта — сетка клеток поверх тех же координат, что уже отвечают за рельеф
// (см. TerrainSystem). Проверяется размер сетки, попадание поселения в свою
// клетку, выбор среди нескольких поселений и владельца среди нескольких держав
public class WorldMapSystemTests
{
    [Fact]
    public void Build_ReturnsRequestedDimensions()
    {
        var world = new World();

        var map = WorldMapSystem.Build(world, width: 10, height: 5);

        Assert.Equal(10, map.Width);
        Assert.Equal(5, map.Height);
        Assert.Equal(5, map.Cells.Length);
        Assert.All(map.Cells, row => Assert.Equal(10, row.Length));
    }

    [Fact]
    public void Build_ReliefMatchesTerrainSystem_AndIsDeterministic()
    {
        var world = new World { Seed = 7 };

        var first = WorldMapSystem.Build(world, width: 10, height: 10);
        var second = WorldMapSystem.Build(world, width: 10, height: 10);

        for (var row = 0; row < 10; row++)
        {
            for (var col = 0; col < 10; col++)
            {
                Assert.Equal(first.Cells[row][col].Relief, second.Cells[row][col].Relief);
            }
        }
    }

    [Fact]
    public void Build_PlacesSettlementInItsCell()
    {
        var world = new World();
        var settlement = BuildPopulatedSettlement(1, x: 750, y: 250);
        world.Settlements.Add(settlement);

        // Сетка 10x10 над картой 1000x1000 — клетка 100х100, поселение на (750,250)
        // должно оказаться в клетке (col=7, row=2)
        var map = WorldMapSystem.Build(world, width: 10, height: 10);

        Assert.Equal(settlement, map.Cells[2][7].Settlement);
    }

    [Fact]
    public void Build_EmptySettlement_IsNotPlaced()
    {
        var world = new World();
        var settlement = new Settlement { Id = 1, Name = "Пустое", X = 500, Y = 500 };
        world.Settlements.Add(settlement);

        var map = WorldMapSystem.Build(world, width: 10, height: 10);

        Assert.DoesNotContain(map.Cells.SelectMany(r => r), c => c.Settlement == settlement);
    }

    [Fact]
    public void Build_TwoSettlementsInOneCell_KeepsTheMorePopulous()
    {
        var world = new World();

        var small = BuildPopulatedSettlement(1, x: 510, y: 510, residents: 2);
        var big = BuildPopulatedSettlement(2, x: 520, y: 520, residents: 8);

        world.Settlements.Add(small);
        world.Settlements.Add(big);

        var map = WorldMapSystem.Build(world, width: 10, height: 10);

        Assert.Equal(big, map.Cells[5][5].Settlement);
    }

    [Fact]
    public void Build_ContestedSettlement_OwnerIsTheOneWithMoreControl()
    {
        var world = new World();

        var disputed = BuildPopulatedSettlement(1, x: 500, y: 500);
        world.Settlements.Add(disputed);

        var nearCapital = new Settlement { Id = 2, Name = "Близко", X = 510, Y = 500 };
        var farCapital = new Settlement { Id = 3, Name = "Далеко", X = 990, Y = 500 };
        world.Settlements.Add(nearCapital);
        world.Settlements.Add(farCapital);

        // Id нарочно противоположны ожидаемому порядку по контролю — чтобы тест
        // не мог случайно совпасть с сортировкой по Id вместо сортировки по GetControl
        var strong = new Kingdom
        {
            Id = 9, Name = "Сильное", FoundedYear = 1,
            Dynasty = new Dynasty { Id = 1, Name = "Дом1", FoundedYear = 1, Founder = disputed.Members[0] },
            Ruler = disputed.Members[0],
            Settlements = [nearCapital, disputed],
            Capital = nearCapital
        };

        var weak = new Kingdom
        {
            Id = 1, Name = "Слабое", FoundedYear = 1,
            Dynasty = new Dynasty { Id = 2, Name = "Дом2", FoundedYear = 1, Founder = disputed.Members[0] },
            Ruler = disputed.Members[0],
            Settlements = [farCapital, disputed],
            Capital = farCapital
        };

        world.Kingdoms.Add(strong);
        world.Kingdoms.Add(weak);

        var map = WorldMapSystem.Build(world, width: 10, height: 10);

        Assert.Equal(strong, map.Cells[5][5].Owner);
    }

    [Fact]
    public void Build_UnclaimedSettlement_HasNoOwner()
    {
        var world = new World();
        var settlement = BuildPopulatedSettlement(1, x: 500, y: 500);
        world.Settlements.Add(settlement);

        var map = WorldMapSystem.Build(world, width: 10, height: 10);

        Assert.Null(map.Cells[5][5].Owner);
    }

    private static Settlement BuildPopulatedSettlement(int id, double x, double y, int residents = 1)
    {
        var settlement = new Settlement { Id = id, Name = $"Поселение{id}", X = x, Y = y };

        for (var i = 0; i < residents; i++)
        {
            settlement.Members.Add(new Character
            {
                Id = id * 1000 + i,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = "Фермер"
            });
        }

        return settlement;
    }
}
