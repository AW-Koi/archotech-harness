using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Spawns a thing or item stack on a cell.</summary>
public static class SpawnThingCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var spawned = Place(ThingFactory.Make(args), Arguments.Cell(args, "cell", map), map, Arguments.Int(args, "count", 1));
        return CommandResult.Done(new JsonObject
        {
            ["id"] = spawned.ThingID,
            ["def"] = spawned.def.defName,
            ["cell"] = Arguments.Json(spawned.Position),
            ["count"] = spawned.stackCount,
        });
    }

    /// <summary>Places the thing on or near the cell, owned by the player when it can have a faction.</summary>
    public static Thing Place(Thing thing, IntVec3 cell, Map map, int count = 1)
    {
        thing.stackCount = System.Math.Min(count, thing.def.stackLimit);
        if (thing.def.CanHaveFaction)
        {
            thing.SetFactionDirect(Faction.OfPlayer);
        }

        return GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near, out var placed)
            ? placed
            : throw new HarnessException($"Could not place {thing.def.defName} near {cell}.");
    }

    /// <summary>Spawns the building on the cell with the rotation, owned by the player, replacing what is in the way.</summary>
    /// <remarks>Throws only when the footprint leaves the map.</remarks>
    public static Thing PlaceExactly(Thing building, IntVec3 cell, Rot4 rotation, Map map)
    {
        var footprint = GenAdj.OccupiedRect(cell, rotation, building.def.Size);
        if (!footprint.InBounds(map))
        {
            throw new HarnessException($"{building.def.defName} at {cell} facing {rotation.ToStringHuman()} reaches off the map.");
        }

        SpawnConflict.ClearFor(footprint, map);

        if (building.def.CanHaveFaction)
        {
            building.SetFactionDirect(Faction.OfPlayer);
        }

        return GenSpawn.Spawn(building, cell, map, rotation);
    }
}
