using System.Runtime.CompilerServices;
using ArchotechHarness.Commands;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Builds finished structures, zones and ground instantly.</summary>
public static class Build
{
    /// <summary>Starts a walled room; set its Size or Covering bounds and finish with Build().</summary>
    public static RoomBuilder Room([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new($"{System.IO.Path.GetFileName(file)}:{line}");

    /// <summary>Starts a stockpile zone over the area; finish with Build().</summary>
    public static StockpileBuilder Stockpile(CellRect area, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(area, $"{System.IO.Path.GetFileName(file)}:{line}");

    /// <summary>Starts a growing zone over the area; set what it is Planting and finish with Build().</summary>
    public static GrowingZoneBuilder GrowingZone(CellRect area, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(area, $"{System.IO.Path.GetFileName(file)}:{line}");

    /// <summary>Starts an L-shaped path, along x then along z; set what it is Of and finish with Build().</summary>
    public static PathBuilder Path(IntVec3 from, IntVec3 to, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(from, to, $"{System.IO.Path.GetFileName(file)}:{line}");

    /// <summary>Replaces a wall cell with a wall-mounted building such as a cooler or vent; throws when the cell holds no wall.</summary>
    public static Thing InWall(ThingDef def, IntVec3 cell, Rot4 facing)
    {
        var map = World.Map;
        if (cell.GetEdifice(map) is not { def.IsWall: true } wall)
        {
            throw new HarnessException($"Build.InWall({def.defName}) needs a wall at {cell}.");
        }

        wall.Destroy();
        var thing = ThingFactory.Make(def);
        thing.SetFactionDirect(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, facing);
    }

    /// <summary>Lays a visible L-shaped power conduit, along x then along z, such as from a generator to a powered room.</summary>
    public static void Conduit(IntVec3 from, IntVec3 to) => PowerConduits.Lay(World.Map, PathCells.Between(from, to, 1), ThingDefOf.PowerConduit);

    /// <summary>Lays the floor over the area, such as a path or patio.</summary>
    public static void Floor(CellRect area, TerrainDef floor)
    {
        var map = World.Map;
        foreach (var cell in area.ClipInsideMap(map))
        {
            map.terrainGrid.SetTerrain(cell, floor);
        }
    }

    /// <summary>Makes the area open, walkable and unroofed: removes everything but the player's pawns, and swaps water or impassable ground for soil.</summary>
    public static void Clearing(CellRect area)
    {
        var map = World.Map;
        var inside = area.ClipInsideMap(map);
        PawnSweep.Remove(PawnSweep.Strangers(map, inside));
        BuildRoomCommand.Clear(map, inside);
    }
}
