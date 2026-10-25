using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DevHarness.Commands;
using Verse;

namespace DevHarness.Probes;

/// <summary>Probes that ship with the harness: filthCount, thingCount, pawn, mapLoaded and tick.</summary>
public static class BuiltInProbes
{
    public static IEnumerable<KeyValuePair<string, Func<JsonObject, JsonNode>>> All => new Dictionary<string, Func<JsonObject, JsonNode>>
    {
        ["filthCount"] = FilthCount,
        ["thingCount"] = ThingCount,
        ["pawn"] = PawnState,
        ["mapLoaded"] = _ => Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null,
        ["tick"] = _ => Find.TickManager?.TicksGame ?? 0,
    };

    /// <summary>Counts filth, optionally limited to "rect" and to the defNames in "defs".</summary>
    private static JsonNode FilthCount(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var filth = Within(args, map, map.listerThings.ThingsInGroup(ThingRequestGroup.Filth));
        var defs = Arguments.Strings(args, "defs").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (defs.Count > 0)
        {
            filth = filth.Where(t => defs.Contains(t.def.defName)).ToList();
        }

        var byDef = new JsonObject();
        foreach (var group in filth.GroupBy(t => t.def.defName).OrderBy(g => g.Key))
        {
            byDef[group.Key] = group.Count();
        }

        return new JsonObject { ["count"] = filth.Count, ["byDef"] = byDef };
    }

    private static JsonNode ThingCount(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var def = Arguments.Def<ThingDef>(Arguments.RequiredString(args, "def"));
        var things = Within(args, map, map.listerThings.ThingsOfDef(def));
        return new JsonObject { ["count"] = things.Count, ["stackTotal"] = things.Sum(t => t.stackCount) };
    }

    private static JsonNode PawnState(JsonObject args)
    {
        var pawn = Arguments.Pawn(args);
        return new JsonObject
        {
            ["id"] = pawn.ThingID,
            ["name"] = pawn.LabelShort,
            ["cell"] = Arguments.Json(pawn.Position),
            ["job"] = pawn.CurJobDef?.defName,
            ["jobReport"] = pawn.CurJob != null ? pawn.jobs.curDriver?.GetReport() : null,
            ["primary"] = pawn.equipment?.Primary?.def.defName,
            ["apparel"] = new JsonArray(pawn.apparel?.WornApparel.Select(a => (JsonNode)a.def.defName).ToArray() ?? Array.Empty<JsonNode>()),
            ["drafted"] = pawn.Drafted,
            ["downed"] = pawn.Downed,
            ["work"] = SetWorkCommand.WorkPriorities(pawn),
        };
    }

    private static List<Thing> Within(JsonObject args, Map map, IEnumerable<Thing> things)
    {
        if (args["rect"] == null)
        {
            return things.ToList();
        }

        var rect = Arguments.Rect(args, "rect", map);
        return things.Where(t => rect.Contains(t.Position)).ToList();
    }
}
