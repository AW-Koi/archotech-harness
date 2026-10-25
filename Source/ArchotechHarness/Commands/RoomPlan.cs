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

    public IntVec3? Door { get; init; }

    public ThingDef DoorDef { get; init; } = ThingDefOf.Door;

    public bool Roof { get; init; }

    public bool Home { get; init; } = true;
}
