using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Spawns a thing or item stack on a cell.</summary>
public static class SpawnThingCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var cell = Arguments.Cell(args, "cell", map);
        var thing = ThingFactory.Make(args);
        thing.stackCount = System.Math.Min(Arguments.Int(args, "count", 1), thing.def.stackLimit);
        if (thing.def.CanHaveFaction)
        {
            thing.SetFactionDirect(RimWorld.Faction.OfPlayer);
        }

        var spawned = GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near, out var placed)
            ? placed
            : throw new HarnessException($"Could not place {thing.def.defName} near {cell}.");
        return CommandResult.Done(new JsonObject
        {
            ["id"] = spawned.ThingID,
            ["def"] = spawned.def.defName,
            ["cell"] = Arguments.Json(spawned.Position),
            ["count"] = spawned.stackCount,
        });
    }
}
