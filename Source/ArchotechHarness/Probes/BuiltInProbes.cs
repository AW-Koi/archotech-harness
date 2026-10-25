using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ArchotechHarness.Commands;
using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Probes;

/// <summary>Probes that ship with the harness: filthCount, thingCount, pawn, thoughts, floatMenu, mapLoaded and tick.</summary>
public static class BuiltInProbes
{
    public static IEnumerable<KeyValuePair<string, Func<JsonObject, JsonNode>>> All => new Dictionary<string, Func<JsonObject, JsonNode>>
    {
        ["filthCount"] = FilthCount,
        ["thingCount"] = ThingCount,
        ["pawn"] = PawnState,
        ["thoughts"] = Thoughts,
        ["floatMenu"] = FloatMenu,
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

    /// <summary>The pawn's memory thoughts, optionally only "def": def, mood offset and the other pawn for social memories.</summary>
    private static JsonNode Thoughts(JsonObject args)
    {
        var memories = Arguments.Pawn(args).Memories(Arguments.String(args, "def"));
        return new JsonObject
        {
            ["has"] = memories.Count > 0,
            ["memories"] = new JsonArray(memories.Select(m => (JsonNode)new JsonObject
            {
                ["def"] = m.def.defName,
                ["mood"] = m.MoodOffset(),
                ["otherPawn"] = m.otherPawn?.LabelShort,
            }).ToArray()),
        };
    }

    /// <summary>The right-click options the pawn would see at "cell", optionally only labels containing "labelContains".</summary>
    private static JsonNode FloatMenu(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var pawn = Arguments.Pawn(args);
        var cell = Arguments.Cell(args, "cell", map);
        var labelContains = Arguments.String(args, "labelContains");
        var options = pawn.FloatMenuAt(cell)
            .Where(o => labelContains == null || o.Label.IndexOf(labelContains, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        return new JsonObject
        {
            ["enabledCount"] = options.Count(o => !o.Disabled),
            ["disabledCount"] = options.Count(o => o.Disabled),
            ["options"] = new JsonArray(options.Select(o => (JsonNode)new JsonObject
            {
                ["label"] = o.Label,
                ["disabled"] = o.Disabled,
            }).ToArray()),
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
