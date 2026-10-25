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

        var room = new RoomPlan(rect)
        {
            Wall = Arguments.Def<ThingDef>(Arguments.String(args, "wall", ThingDefOf.Wall.defName)),
            Stuff = Arguments.Def<ThingDef>(Arguments.String(args, "stuff", ThingDefOf.WoodLog.defName)),
            Floor = Arguments.String(args, "floor") is { } floorName ? Arguments.Def<TerrainDef>(floorName) : null,
            Door = args["door"] is { } doorNode ? Arguments.ToCell(doorNode, map, "door") : null,
            DoorDef = Arguments.Def<ThingDef>(Arguments.String(args, "doorDef", ThingDefOf.Door.defName)),
            Roof = Arguments.Bool(args, "roof", false),
            Home = Arguments.Bool(args, "home", true),
        };
        Build(map, room);
        var interior = rect.ContractedBy(1);
        return CommandResult.Done(new JsonObject
        {
            ["rect"] = new JsonArray(rect.minX, rect.minZ, rect.Width, rect.Height),
            ["interior"] = new JsonArray(interior.minX, interior.minZ, interior.Width, interior.Height),
            ["center"] = Arguments.Json(rect.CenterCell),
        });
    }

    public static void Build(Map map, RoomPlan room)
    {
        Clear(map, room.Rect.ExpandedBy(1).ClipInsideMap(map));
        foreach (var cell in room.Rect.EdgeCells)
        {
            var def = cell == room.Door ? room.DoorDef : room.Wall;
            var building = ThingMaker.MakeThing(def, def.MadeFromStuff ? room.Stuff ?? GenStuff.DefaultStuffFor(def) : null);
            building.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(building, cell, map);
        }

        foreach (var cell in room.Rect.ContractedBy(1))
        {
            if (room.Floor != null)
            {
                map.terrainGrid.SetTerrain(cell, room.Floor);
            }

            if (room.Roof)
            {
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }

            foreach (var filth in cell.GetThingList(map).OfType<Filth>().ToList())
            {
                filth.Destroy();
            }
        }

        if (room.Home)
        {
            foreach (var cell in room.Rect)
            {
                map.areaManager.Home[cell] = true;
            }
        }
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
