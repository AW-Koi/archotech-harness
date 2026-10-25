using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>What buildRoom builds: the rectangle includes the walls, and stuff applies to anything made from stuff.</summary>
public sealed class RoomPlan(CellRect rect)
{
    public CellRect Rect { get; } = rect;

    public ThingDef Wall { get; init; } = ThingDefOf.Wall;

    public ThingDef Stuff { get; init; } = ThingDefOf.WoodLog;

    public TerrainDef Floor { get; init; }

    public IReadOnlyCollection<IntVec3> Doors { get; init; } = [];

    public ThingDef DoorDef { get; init; } = ThingDefOf.Door;

    public bool Roof { get; init; }

    public bool Home { get; init; } = true;

    /// <summary>Lays conduits under the walls and doors: hidden ones where the game has them.</summary>
    public bool Powered { get; init; }

    /// <summary>Clears only inside the rectangle and keeps doors already on its walls.</summary>
    public bool Adjoining { get; init; }
}
