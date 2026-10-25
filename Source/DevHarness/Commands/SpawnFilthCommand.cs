using System;
using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace DevHarness.Commands;

/// <summary>Spawns filth on listed cells, or on "count" random standable cells of a rectangle.</summary>
/// <remarks>Random picks use "seed" (default 1), so a scenario places the same filth every run.</remarks>
public static class SpawnFilthCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var def = Arguments.Def<ThingDef>(Arguments.String(args, "def", "Filth_Dirt"));
        if (def.filth == null)
        {
            throw new HarnessException($"{def.defName} is not filth.");
        }

        var thickness = Arguments.Int(args, "thickness", 1);
        var cells = args["cells"] is JsonArray list
            ? list.Select(node => Arguments.ToCell(node!, map, "cells")).ToList()
            : PickCells(args, map);

        var spawned = cells.Count(cell => FilthMaker.TryMakeFilth(cell, map, def, thickness, shouldPropagate: false));
        return CommandResult.Done(new JsonObject { ["def"] = def.defName, ["spawned"] = spawned });
    }

    private static System.Collections.Generic.List<IntVec3> PickCells(JsonObject args, Map map)
    {
        var rect = Arguments.Rect(args, "rect", map);
        var candidates = rect.Cells.Where(c => c.Standable(map)).ToList();
        var random = new Random(Arguments.Int(args, "seed", 1));
        var count = Math.Min(Arguments.Int(args, "count", candidates.Count), candidates.Count);
        return candidates.OrderBy(_ => random.Next()).Take(count).ToList();
    }
}
