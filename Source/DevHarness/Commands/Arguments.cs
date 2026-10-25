using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Commands;

/// <summary>Reads typed command arguments and resolves them to game objects.</summary>
/// <remarks>Throws HarnessException with a readable message on bad input.</remarks>
public static class Arguments
{
    public static string String(JsonObject args, string key, string fallback = null) =>
        args[key] is JsonValue value ? value.ToString() : fallback;

    public static string RequiredString(JsonObject args, string key) =>
        String(args, key) ?? throw new HarnessException($"Missing \"{key}\".");

    public static int Int(JsonObject args, string key, int fallback) =>
        args[key] is JsonValue value ? (int)value.GetValue<double>() : fallback;

    public static float Float(JsonObject args, string key, float fallback) =>
        args[key] is JsonValue value ? (float)value.GetValue<double>() : fallback;

    public static bool Bool(JsonObject args, string key, bool fallback) =>
        args[key] is JsonValue value ? value.GetValue<bool>() : fallback;

    public static IEnumerable<string> Strings(JsonObject args, string key) => args[key] switch
    {
        JsonArray array => array.Select(item => item!.ToString()),
        JsonValue value => new[] { value.ToString() },
        _ => Enumerable.Empty<string>(),
    };

    public static Map CurrentMap() =>
        Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null
            ? Find.CurrentMap
            : throw new HarnessException("No map is loaded yet.");

    /// <summary>Reads a cell given as [x, z], {"x":, "z":} or "center".</summary>
    public static IntVec3 Cell(JsonObject args, string key, Map map)
    {
        var node = args[key] ?? throw new HarnessException($"Missing \"{key}\".");
        return ToCell(node, map, key);
    }

    public static IntVec3 ToCell(JsonNode node, Map map, string key)
    {
        IntVec3 cell = node switch
        {
            JsonValue v when v.GetValueKind() == JsonValueKind.String && v.ToString() == "center" => map.Center,
            JsonArray a when a.Count == 2 => new IntVec3((int)a[0]!.GetValue<double>(), 0, (int)a[1]!.GetValue<double>()),
            JsonObject o => new IntVec3(Int(o, "x", 0), 0, Int(o, "z", 0)),
            _ => throw new HarnessException($"\"{key}\" must be [x, z], {{\"x\":, \"z\":}} or \"center\"."),
        };
        if (!cell.InBounds(map))
        {
            throw new HarnessException($"\"{key}\" {cell} is outside the {map.Size.x}x{map.Size.z} map.");
        }

        return cell;
    }

    /// <summary>Reads a rectangle given as [x, z, width, height] or {"x","z","width","height"}.</summary>
    public static CellRect Rect(JsonObject args, string key, Map map)
    {
        var rect = args[key] switch
        {
            JsonArray a when a.Count == 4 => new CellRect(
                (int)a[0]!.GetValue<double>(), (int)a[1]!.GetValue<double>(), (int)a[2]!.GetValue<double>(), (int)a[3]!.GetValue<double>()),
            JsonObject o => new CellRect(Int(o, "x", 0), Int(o, "z", 0), Int(o, "width", 1), Int(o, "height", 1)),
            null => throw new HarnessException($"Missing \"{key}\"."),
            _ => throw new HarnessException($"\"{key}\" must be [x, z, width, height]."),
        };
        if (!rect.InBounds(map))
        {
            throw new HarnessException($"\"{key}\" {rect} is outside the map.");
        }

        return rect;
    }

    public static T Def<T>(string defName) where T : Def
    {
        var def = DefDatabase<T>.GetNamedSilentFail(defName)
            ?? DefDatabase<T>.AllDefsListForReading.FirstOrDefault(d => string.Equals(d.label, defName, StringComparison.OrdinalIgnoreCase));
        return def ?? throw new HarnessException($"No {typeof(T).Name} named '{defName}'.");
    }

    /// <summary>Finds a spawned pawn on the current map by nickname, full name or ThingID.</summary>
    public static Pawn Pawn(JsonObject args, string key = "pawn")
    {
        var wanted = RequiredString(args, key);
        var map = CurrentMap();
        return map.mapPawns.AllPawnsSpawned.FirstOrDefault(p =>
                p.ThingID == wanted
                || string.Equals(p.LabelShort, wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Name?.ToStringFull, wanted, StringComparison.OrdinalIgnoreCase))
            ?? throw new HarnessException($"No spawned pawn called '{wanted}'.");
    }

    public static JsonArray Json(IntVec3 cell) => new(cell.x, cell.z);
}
