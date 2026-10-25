using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Clears a rectangle and builds finished walls, an optional door, floor and roof around it.</summary>
/// <remarks>The rectangle includes the walls. The room becomes home area unless "home" is false.</remarks>
public static class BuildRoomCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var rect = Arguments.Rect(args, "rect", map);
        if (rect.Width < 3 || rect.Height < 3)
        {
            throw new HarnessException("A room needs at least a 3x3 rectangle.");
        }

        var wallDef = Arguments.Def<ThingDef>(Arguments.String(args, "wall", "Wall"));
        var stuffName = Arguments.String(args, "stuff", "WoodLog");
        var stuff = wallDef.MadeFromStuff ? Arguments.Def<ThingDef>(stuffName) : null;
        var floor = Arguments.String(args, "floor") is { } floorName ? Arguments.Def<TerrainDef>(floorName) : null;
        IntVec3? door = args["door"] is { } doorNode ? Arguments.ToCell(doorNode, map, "door") : null;

        Clear(map, rect.ExpandedBy(1).ClipInsideMap(map));
        foreach (var cell in rect.EdgeCells)
        {
            var def = cell == door ? Arguments.Def<ThingDef>(Arguments.String(args, "doorDef", "Door")) : wallDef;
            var building = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null);
            building.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(building, cell, map);
        }

        var interior = rect.ContractedBy(1);
        foreach (var cell in interior)
        {
            if (floor != null)
            {
                map.terrainGrid.SetTerrain(cell, floor);
            }

            if (Arguments.Bool(args, "roof", false))
            {
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }
        }

        if (Arguments.Bool(args, "home", true))
        {
            foreach (var cell in rect)
            {
                map.areaManager.Home[cell] = true;
            }
        }

        return CommandResult.Done(new JsonObject
        {
            ["rect"] = new JsonArray(rect.minX, rect.minZ, rect.Width, rect.Height),
            ["interior"] = new JsonArray(interior.minX, interior.minZ, interior.Width, interior.Height),
            ["center"] = Arguments.Json(rect.CenterCell),
        });
    }

    private static void Clear(Map map, CellRect area)
    {
        foreach (var cell in area)
        {
            foreach (var thing in cell.GetThingList(map).Where(t => t is not Pawn && t.def.destroyable).ToList())
            {
                thing.Destroy();
            }

            var terrain = cell.GetTerrain(map);
            if (terrain.IsWater || terrain.passability == Traversability.Impassable)
            {
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }

            map.roofGrid.SetRoof(cell, null);
            map.fogGrid.Unfog(cell);
        }
    }
}
