using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Spawns filth on listed cells, or on "count" random standable cells of a rectangle.</summary>
/// <remarks>Random picks use "seed" (default 1), so a scenario places the same filth every run.</remarks>
public static class SpawnFilthCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var def = Arguments.Def<ThingDef>(Arguments.String(args, "def", ThingDefOf.Filth_Dirt.defName));
        var cells = args["cells"] is JsonArray list
            ? list.Select(node => Arguments.ToCell(node!, map, "cells")).ToList()
            : PickCells(map, Arguments.Rect(args, "rect", map), Arguments.Int(args, "count", int.MaxValue), Arguments.Int(args, "seed", 1));

        var spawned = Spawn(map, def, cells, Arguments.Int(args, "thickness", 1));
        return CommandResult.Done(new JsonObject { ["def"] = def.defName, ["spawned"] = spawned.Count });
    }

    /// <summary>Makes filth on each cell that accepts it; returns the filth made or thickened.</summary>
    public static IReadOnlyList<Filth> Spawn(Map map, ThingDef def, IEnumerable<IntVec3> cells, int thickness = 1)
    {
        if (def.filth == null)
        {
            throw new HarnessException($"{def.defName} is not filth.");
        }

        var spawned = new List<Filth>();
        foreach (var cell in cells)
        {
            if (FilthMaker.TryMakeFilth(cell, map, def, out var filth, thickness, shouldPropagate: false) && filth != null)
            {
                spawned.Add(filth);
            }
        }

        return spawned;
    }

    /// <summary>Picks up to count random standable cells; the same seed picks the same cells.</summary>
    public static IReadOnlyList<IntVec3> PickCells(Map map, CellRect rect, int count, int seed)
    {
        var candidates = rect.Cells.Where(c => c.Standable(map)).ToList();
        var random = new Random(seed);
        return candidates.OrderBy(_ => random.Next()).Take(Math.Min(count, candidates.Count)).ToList();
    }
}
