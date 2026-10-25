using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Plans a bed with its head against the wall opposite the door, an end table either side and a dresser.</summary>
/// <remarks>Best-effort: each piece falls back to corners, wall-side cells and free floor, and is left out when none is free.</remarks>
public static class BedroomLayout
{
    private static readonly IntVec2 SingleCell = new(1, 1);

    /// <summary>Occupies what it plans and keeps the bed's sides and foot clear.</summary>
    public static BedroomPlan Plan(RoomGrid grid, IntVec2 bedSize, IntVec2 dresserSize)
    {
        var bed = DecorLayout.Fit(grid, bedSize, rotatable: true, Preferred(grid, bedSize));
        var tables = new List<Placement>();
        if (bed != null)
        {
            var flanks = Flanks(bed.Rect, bed.Rotation.Opposite);
            var clearance = Beside(bed.Rect, bed.Rotation)
                .Concat(Beside(bed.Rect, bed.Rotation.Rotated(RotationDirection.Clockwise)))
                .Concat(Beside(bed.Rect, bed.Rotation.Rotated(RotationDirection.Counterclockwise)))
                .Where(c => !flanks.Contains(c));
            foreach (var cell in clearance)
            {
                grid.Reserve(cell);
            }

            foreach (var flank in flanks)
            {
                var preferred = new Placement(flank, bed.Rotation, CellRect.SingleCell(flank));
                if (DecorLayout.Fit(grid, SingleCell, rotatable: true, [preferred]) is { } table)
                {
                    tables.Add(table);
                }
            }
        }

        var nearBed = bed?.Rect.CenterCell ?? grid.Interior.CenterCell;
        var dresser = DecorLayout.Nearest(grid, dresserSize, rotatable: true, nearBed);
        return new BedroomPlan(bed, tables, dresser);
    }

    /// <summary>Spots against the wall opposite the door, middle first, with room for end tables and a clear foot.</summary>
    private static IEnumerable<Placement> Preferred(RoomGrid grid, IntVec2 bedSize)
    {
        var rotation = grid.DoorSide;
        var headWall = rotation.Opposite;
        var footprint = Placement.Footprint(bedSize, rotation);
        var middle = grid.Interior.CenterCell;
        return grid.RectsAgainst(headWall, footprint)
            .OrderBy(r => r.CenterCell.DistanceToSquared(middle))
            .Where(r => grid.IsFree(r) && Flanks(r, headWall).All(grid.IsFree) && Beside(r, rotation).All(grid.IsFree))
            .Select(r => Placement.Covering(r, rotation, bedSize));
    }

    /// <summary>The cells either side of the bed's head row, along the head wall.</summary>
    private static List<IntVec3> Flanks(CellRect bed, Rot4 headWall) => headWall.AsInt switch
    {
        0 => [new IntVec3(bed.minX - 1, 0, bed.maxZ), new IntVec3(bed.maxX + 1, 0, bed.maxZ)],
        2 => [new IntVec3(bed.minX - 1, 0, bed.minZ), new IntVec3(bed.maxX + 1, 0, bed.minZ)],
        1 => [new IntVec3(bed.maxX, 0, bed.minZ - 1), new IntVec3(bed.maxX, 0, bed.maxZ + 1)],
        _ => [new IntVec3(bed.minX, 0, bed.minZ - 1), new IntVec3(bed.minX, 0, bed.maxZ + 1)],
    };

    /// <summary>The cells just outside the rect on that side.</summary>
    private static IEnumerable<IntVec3> Beside(CellRect rect, Rot4 side) => rect.Cells
        .Select(c => c + side.FacingCell)
        .Where(c => !rect.Contains(c));
}
