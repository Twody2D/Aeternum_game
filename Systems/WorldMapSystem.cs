using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// ASCII-карта мира. Координаты поселений (Settlement.X/Y) существовали с самого
// начала и до сих пор служили только счётным материалом — расстоянием для
// миграции, соседством для эпидемий, дальностью для дани. Самого мира —
// очертаний королевств, того, где горы, а где низины, — увидеть было нельзя.
//
// Ничего не хранится и здесь: клетка карты — это то же самое, что уже умеет
// TerrainSystem, только с шагом в клетку сетки, а не в отдельную точку.
// Собирает клетки эта система (в WorldMap — Settlement/Kingdom по ссылке,
// не по имени), а превращает их в текст с русскими подписями — Program.cs,
// тем же разделением, что и у остальных отчётов (см. Models/Reports.cs)
public static class WorldMapSystem
{
    public const int DefaultWidth = 60;
    public const int DefaultHeight = 30;

    public static WorldMap Build(World world, int width = DefaultWidth, int height = DefaultHeight)
    {
        var cellWidth = ClimateSystem.MapSize / width;
        var cellHeight = ClimateSystem.MapSize / height;

        // В одну клетку низкого разрешения может попасть несколько поселений —
        // оставляем самое многолюдное, иначе тесная кучка колоний бессмысленно
        // перекрывала бы друг друга на одном знаке
        var settlementByCell = world.Settlements
            .Where(s => s.Members.Any(m => m.Alive))
            .GroupBy(s => ToCell(s, cellWidth, cellHeight, width, height))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Members.Count(m => m.Alive)).First());

        var cells = new MapCell[height][];

        for (var row = 0; row < height; row++)
        {
            cells[row] = new MapCell[width];

            for (var col = 0; col < width; col++)
            {
                var centerX = (col + 0.5) * cellWidth;
                var centerY = (row + 0.5) * cellHeight;
                var relief = TerrainSystem.GetRelief(centerX, centerY, world.Seed);

                settlementByCell.TryGetValue((col, row), out var settlement);

                cells[row][col] = new MapCell(relief, settlement, settlement == null ? null : GetOwner(settlement, world));
            }
        }

        return new WorldMap(width, height, cells);
    }

    private static (int Col, int Row) ToCell(Settlement settlement, double cellWidth, double cellHeight, int width, int height)
    {
        var col = Math.Clamp((int)(settlement.X / cellWidth), 0, width - 1);
        var row = Math.Clamp((int)(settlement.Y / cellHeight), 0, height - 1);

        return (col, row);
    }

    // Государство, чьё присутствие в поселении заметнее прочих — тот же довод
    // о заметном присутствии, на котором уже стоит территориальный контроль
    // (см. KingdomSystem), только выбирает не факт владения, а конкретного
    // владельца среди нескольких претендентов на спорную землю
    private static Kingdom? GetOwner(Settlement settlement, World world)
    {
        return world.Kingdoms
            .Where(k => k.FallenYear == null && k.Settlements.Contains(settlement))
            .OrderByDescending(k => CapitalSystem.GetControl(k, settlement))
            .FirstOrDefault();
    }
}
