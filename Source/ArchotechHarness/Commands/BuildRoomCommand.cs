using System.Collections.Generic;
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
            Doors = args["door"] is { } doorNode ? [Arguments.ToCell(doorNode, map, "door")] : [],
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
        var keptDoors = room.Adjoining ? room.Rect.EdgeCells.Where(c => c.GetDoor(map) != null).ToHashSet() : [];
        Clear(map, room.Adjoining ? room.Rect.ClipInsideMap(map) : room.Rect.ExpandedBy(1).ClipInsideMap(map), keptDoors);
        if (room.Blueprint)
        {
            PlaceBlueprints(map, room, keptDoors);
            return;
        }

        foreach (var cell in room.Rect.EdgeCells.Where(c => !keptDoors.Contains(c)))
        {
            var def = room.Doors.Contains(cell) ? room.DoorDef : room.Wall;
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

        if (room.Powered)
        {
            PowerConduits.Lay(map, room.Rect.EdgeCells, ThingDefOf.HiddenConduit ?? ThingDefOf.PowerConduit);
        }

        if (room.Home)
        {
            foreach (var cell in room.Rect)
            {
                map.areaManager.Home[cell] = true;
            }
        }
    }

    /// <summary>Places wall, door, floor and conduit blueprints, marks the roof to build and the home area, and clears filth.</summary>
    private static void PlaceBlueprints(Map map, RoomPlan room, HashSet<IntVec3> keptDoors)
    {
        foreach (var cell in room.Rect.EdgeCells.Where(c => !keptDoors.Contains(c)))
        {
            var def = room.Doors.Contains(cell) ? room.DoorDef : room.Wall;
            GenConstruct.PlaceBlueprintForBuild(def, cell, map, Rot4.North, Faction.OfPlayer, def.MadeFromStuff ? room.Stuff ?? GenStuff.DefaultStuffFor(def) : null);
            if (room.Powered)
            {
                GenConstruct.PlaceBlueprintForBuild(ThingDefOf.HiddenConduit ?? ThingDefOf.PowerConduit, cell, map, Rot4.North, Faction.OfPlayer, null);
            }
        }

        foreach (var cell in room.Rect.ContractedBy(1))
        {
            if (room.Floor != null)
            {
                GenConstruct.PlaceBlueprintForBuild(room.Floor, cell, map, Rot4.North, Faction.OfPlayer, null);
            }

            if (room.Roof)
            {
                map.areaManager.BuildRoof[cell] = true;
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

    /// <summary>Destroys everything but pawns, unroofs and unfogs, and swaps water or impassable ground for soil.</summary>
    public static void Clear(Map map, CellRect area) => Clear(map, area, []);

    private static void Clear(Map map, CellRect area, HashSet<IntVec3> keepThingsOn)
    {
        foreach (var cell in area)
        {
            var things = keepThingsOn.Contains(cell) ? [] : cell.GetThingList(map).Where(t => t is not Pawn && t.def.destroyable).ToList();
            foreach (var thing in things)
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
