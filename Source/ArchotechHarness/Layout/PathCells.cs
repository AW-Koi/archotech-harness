using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>The cells of an L-shaped path: along x from the start, then along z to the end.</summary>
public static class PathCells
{
    public static IReadOnlyList<IntVec3> Between(IntVec3 from, IntVec3 to, int width)
    {
        var corner = new IntVec3(to.x, 0, from.z);
        return Band(from, corner, width).Concat(Band(corner, to, width)).Distinct().ToList();
    }

    private static IEnumerable<IntVec3> Band(IntVec3 a, IntVec3 b, int width)
    {
        var below = (width - 1) / 2;
        var above = width / 2;
        var minX = Math.Min(a.x, b.x) - below;
        var minZ = Math.Min(a.z, b.z) - below;
        return new CellRect(minX, minZ, Math.Max(a.x, b.x) + above - minX + 1, Math.Max(a.z, b.z) + above - minZ + 1).Cells;
    }
}
