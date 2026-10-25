using System;
using System.Collections.Generic;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Random cells within a radius of a target, such as where shells land; the same seed picks the same cells.</summary>
public static class ScatterCells
{
    public static IReadOnlyList<IntVec3> Around(IntVec3 target, float radius, int count, int seed)
    {
        var random = new Random(seed);
        var reach = (int)Math.Floor(radius);
        var cells = new List<IntVec3>(count);
        while (cells.Count < count)
        {
            var offset = new IntVec3(random.Next(-reach, reach + 1), 0, random.Next(-reach, reach + 1));
            if (offset.LengthHorizontalSquared <= radius * radius)
            {
                cells.Add(target + offset);
            }
        }

        return cells;
    }
}
