using System.Linq;
using ArchotechHarness.Commands;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Reads a built room into a RoomGrid and places planned furniture in it.</summary>
internal static class RoomSurvey
{
    /// <summary>Doors from the walls; buildings, items and impassable ground block; interaction cells and bedsides stay clear.</summary>
    public static RoomGrid Grid(HarnessRoom room, Map map)
    {
        var doors = room.Bounds.EdgeCells.Where(c => c.GetDoor(map) != null);
        var blocked = room.Interior.Cells.Where(c => Blocks(c, map));
        var things = room.Interior.Cells.SelectMany(c => c.GetThingList(map)).Where(t => t.def.category == ThingCategory.Building).Distinct().ToList();
        var interactionCells = things.Where(t => t.def.hasInteractionCell).Select(t => t.InteractionCell);
        var bedsides = things.OfType<Building_Bed>().SelectMany(b => GenAdj.CellsAdjacentCardinal(b));
        return new RoomGrid(room.Bounds, doors, blocked, interactionCells.Concat(bedsides));
    }

    /// <summary>Spawns the def at the placement, from the stuff when the def accepts it, owned by the player.</summary>
    public static Thing Place(ThingDef def, ThingDef stuff, Placement placement, Map map)
    {
        var usable = stuff != null && def.MadeFromStuff && GenStuff.AllowedStuffsFor(def).Contains(stuff) ? stuff : null;
        var thing = ThingFactory.Make(def, usable);
        return SpawnThingCommand.PlaceExactly(thing, placement.Cell, def.rotatable ? placement.Rotation : Rot4.North, map);
    }

    /// <summary>A plant pot with the plant fully grown in it.</summary>
    public static Thing PlacePot(ThingDef plant, IntVec3 cell, Map map)
    {
        var pot = Place(FurnitureDefOf.PlantPot, null, new Placement(cell, Rot4.North, CellRect.SingleCell(cell)), map);
        var setup = new ThingSetup { Crop = plant };
        setup.Apply(pot);
        return pot;
    }

    private static bool Blocks(IntVec3 cell, Map map) =>
        cell.GetTerrain(map).passability == Traversability.Impassable
        || cell.GetThingList(map).Any(t => t.def.category is ThingCategory.Building or ThingCategory.Item && t.def.building is not { isPowerConduit: true });
}
