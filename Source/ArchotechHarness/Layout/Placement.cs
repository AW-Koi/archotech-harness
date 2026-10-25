using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Where a thing goes: the cell and rotation to spawn it with, and the cells it then covers.</summary>
public sealed record Placement(IntVec3 Cell, Rot4 Rotation, CellRect Rect)
{
    /// <summary>The placement of a thing of this size and rotation that covers exactly the rect.</summary>
    public static Placement Covering(CellRect rect, Rot4 rotation, IntVec2 size)
    {
        foreach (var cell in rect)
        {
            if (GenAdj.OccupiedRect(cell, rotation, size) == rect)
            {
                return new Placement(cell, rotation, rect);
            }
        }

        throw new HarnessException($"A thing of size {size} facing {rotation} can't cover {rect}.");
    }

    /// <summary>The rect's width and height for a thing of this size and rotation.</summary>
    public static IntVec2 Footprint(IntVec2 size, Rot4 rotation) => rotation.IsHorizontal ? new IntVec2(size.z, size.x) : size;
}
