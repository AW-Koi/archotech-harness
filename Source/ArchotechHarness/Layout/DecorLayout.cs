using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Finds spots for single pieces: preferred spots, then corners, wall-side cells and any free floor.</summary>
/// <remarks>Each method occupies the spots it returns and returns null or fewer pieces when the floor runs out.</remarks>
public static class DecorLayout
{
    private static readonly IntVec2 SingleCell = new(1, 1);

    /// <summary>The first free spot among the preferred ones, then corners, wall-side cells and any free floor.</summary>
    public static Placement Fit(RoomGrid grid, IntVec2 size, bool rotatable, IEnumerable<Placement> preferred)
    {
        var spot = preferred
            .Concat(grid.CornerSpots(size, rotatable))
            .Concat(grid.WallSpots(size, rotatable))
            .Concat(grid.FloorSpots(size, rotatable))
            .FirstOrDefault(p => grid.IsFree(p.Rect));
        if (spot != null)
        {
            grid.Occupy(spot.Rect);
        }

        return spot;
    }

    /// <summary>Adds lamps, against the walls where they can, until every interior cell is within the radius of one.</summary>
    public static IReadOnlyList<Placement> Lamps(RoomGrid grid, IntVec2 size, float radius)
    {
        var dark = grid.Interior.Cells.ToHashSet();
        var lamps = new List<Placement>();
        while (dark.Count > 0)
        {
            var best = Brightest(grid.WallSpots(size, rotatable: true), dark, radius) ?? Brightest(grid.FloorSpots(size, rotatable: true), dark, radius);
            if (best == null)
            {
                break;
            }

            grid.Occupy(best.Rect);
            lamps.Add(best);
            dark.RemoveWhere(c => c.DistanceTo(best.Cell) <= radius);
        }

        return lamps;
    }

    /// <summary>Against the wall nearest the middle of the room where it can, else any free floor; null when none is free.</summary>
    public static Placement Art(RoomGrid grid, IntVec2 size, bool rotatable) =>
        Nearest(grid, size, rotatable, grid.Interior.CenterCell);

    /// <summary>The wall spot nearest the target where it can, else any free floor; null when none is free.</summary>
    public static Placement Nearest(RoomGrid grid, IntVec2 size, bool rotatable, IntVec3 target) =>
        Fit(grid, size, rotatable, grid.WallSpots(size, rotatable).OrderBy(s => s.Rect.CenterCell.DistanceToSquared(target)).ToList());

    /// <summary>Up to count cells for plant pots: corners farthest from the target first, then wall-side cells and free floor.</summary>
    public static IReadOnlyList<IntVec3> Pots(RoomGrid grid, int count, IntVec3 awayFrom)
    {
        var pots = new List<IntVec3>();
        while (pots.Count < count)
        {
            var corners = grid.FreeCorners().OrderByDescending(c => c.DistanceToSquared(awayFrom))
                .Select(c => new Placement(c, Rot4.North, CellRect.SingleCell(c)));
            if (Fit(grid, SingleCell, rotatable: false, corners) is not { } pot)
            {
                break;
            }

            pots.Add(pot.Cell);
        }

        return pots;
    }

    private static Placement Brightest(IEnumerable<Placement> spots, HashSet<IntVec3> dark, float radius) =>
        spots.Select(spot => (Spot: spot, Lit: dark.Count(c => c.DistanceTo(spot.Cell) <= radius)))
            .Where(s => s.Lit > 0)
            .OrderByDescending(s => s.Lit)
            .FirstOrDefault()
            .Spot;
}
