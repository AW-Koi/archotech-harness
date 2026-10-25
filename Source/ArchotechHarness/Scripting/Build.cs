using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Builds finished structures instantly.</summary>
public static class Build
{
    /// <summary>Clears the area and builds a walled room centred on a cell; the size includes the walls.</summary>
    /// <remarks>Walls default to ThingDefOf.Wall and stuff to ThingDefOf.WoodLog. Without a floor, the ground stays.</remarks>
    public static HarnessRoom Room(IntVec3 center, int width, int height, TerrainDef floor = null, ThingDef wall = null,
        ThingDef stuff = null, Rot4? door = null, bool roof = false)
    {
        var bounds = CellRect.CenteredOn(center, width / 2, height / 2);
        bounds = new CellRect(bounds.minX, bounds.minZ, width, height);
        var doorCell = (door ?? Rot4.South).AsInt switch
        {
            0 => new IntVec3(bounds.CenterCell.x, 0, bounds.maxZ),
            1 => new IntVec3(bounds.maxX, 0, bounds.CenterCell.z),
            2 => new IntVec3(bounds.CenterCell.x, 0, bounds.minZ),
            _ => new IntVec3(bounds.minX, 0, bounds.CenterCell.z),
        };
        BuildRoomCommand.Build(World.Map, new RoomPlan(bounds)
        {
            Wall = wall ?? ThingDefOf.Wall,
            Stuff = stuff ?? ThingDefOf.WoodLog,
            Floor = floor,
            Door = doorCell,
            Roof = roof,
        });
        return new HarnessRoom(bounds);
    }
}
