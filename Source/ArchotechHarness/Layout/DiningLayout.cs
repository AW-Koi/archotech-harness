using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Plans a grid of tables over an area, each ringed by chairs facing it.</summary>
public static class DiningLayout
{
    private static readonly IntVec2 SingleCell = new(1, 1);

    /// <summary>Skips chairs and tables on cells that aren't free; throws only when the area leaves the room.</summary>
    public static IReadOnlyList<DiningSet> Plan(RoomGrid grid, CellRect area, IntVec2 tableSize, bool rotatable)
    {
        if (!area.Cells.All(grid.Interior.Contains))
        {
            throw new HarnessException($"The dining area {area} reaches outside the room's floor {grid.Interior}.");
        }

        var orientations = rotatable ? new[] { Rot4.North, Rot4.East } : [Rot4.North];
        var best = orientations
            .Select(rotation => Sets(grid.Copy(), area, tableSize, rotation))
            .OrderByDescending(sets => sets.Count)
            .First();
        foreach (var placement in best.SelectMany(s => s.Chairs.Append(s.Table)))
        {
            grid.Occupy(placement.Rect);
        }

        return best;
    }

    /// <summary>How many tables with their chairs the area holds when nothing is in the way.</summary>
    public static int Capacity(CellRect area, IntVec2 tableSize, bool rotatable) =>
        (rotatable ? new[] { Rot4.North, Rot4.East } : [Rot4.North])
        .Select(r => Placement.Footprint(tableSize, r))
        .Max(f => area.Width / (f.x + 2) * (area.Height / (f.z + 2)));

    private static List<DiningSet> Sets(RoomGrid grid, CellRect area, IntVec2 tableSize, Rot4 rotation)
    {
        var footprint = Placement.Footprint(tableSize, rotation);
        var unit = new IntVec2(footprint.x + 2, footprint.z + 2);
        var across = area.Width / unit.x;
        var up = area.Height / unit.z;
        var originX = area.minX + (area.Width - across * unit.x) / 2;
        var originZ = area.minZ + (area.Height - up * unit.z) / 2;
        var sets = new List<DiningSet>();
        for (var i = 0; i < across; i++)
        {
            for (var j = 0; j < up; j++)
            {
                var table = new CellRect(originX + i * unit.x + 1, originZ + j * unit.z + 1, footprint.x, footprint.z);
                var chairs = Chairs(table).Where(c => grid.IsFree(c.Rect)).ToList();
                if (!grid.IsFree(table) || chairs.Count == 0)
                {
                    continue;
                }

                grid.Occupy(table);
                chairs.ForEach(c => grid.Occupy(c.Rect));
                sets.Add(new DiningSet(Placement.Covering(table, rotation, tableSize), chairs));
            }
        }

        return sets;
    }

    /// <summary>A chair on every cell beside the table's edges, facing it.</summary>
    private static IEnumerable<Placement> Chairs(CellRect table)
    {
        foreach (var side in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
        {
            foreach (var cell in table.Cells.Select(c => c + side.FacingCell).Where(c => !table.Contains(c)))
            {
                yield return Placement.Covering(CellRect.SingleCell(cell), side.Opposite, SingleCell);
            }
        }
    }
}
