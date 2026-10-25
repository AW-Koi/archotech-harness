using System;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>Cells on or toward the edges of a map, for groups that arrive and shells that fly in.</summary>
public static class MapEdge
{
    /// <summary>The cell centred on that side of the map, inset cells in from the edge.</summary>
    public static IntVec3 Entry(Rot4 side, IntVec3 mapSize, int inset) => side.AsInt switch
    {
        0 => new IntVec3(mapSize.x / 2, 0, mapSize.z - 1 - inset),
        1 => new IntVec3(mapSize.x - 1 - inset, 0, mapSize.z / 2),
        2 => new IntVec3(mapSize.x / 2, 0, inset),
        _ => new IntVec3(inset, 0, mapSize.z / 2),
    };

    /// <summary>The cell distance cells from the target in that direction, stopping at the map's edge.</summary>
    public static IntVec3 Toward(IntVec3 target, Rot4 direction, int distance, IntVec3 mapSize)
    {
        var cell = target + direction.FacingCell * distance;
        return new IntVec3(Math.Clamp(cell.x, 0, mapSize.x - 1), 0, Math.Clamp(cell.z, 0, mapSize.z - 1));
    }
}
