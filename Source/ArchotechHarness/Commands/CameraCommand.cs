using System.Text.Json;
using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Jumps the camera to a cell or pawn and sets the zoom.</summary>
/// <remarks>Zoom is close, middle, far, or a root size from 11 (closest) to 60.</remarks>
public static class CameraCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        IntVec3 cell;
        if (args["pawn"] != null)
        {
            var pawn = Arguments.Pawn(args);
            cell = pawn.Position;
            if (Arguments.Bool(args, "select", false))
            {
                Select(pawn);
            }
        }
        else
        {
            cell = Arguments.Cell(args, "cell", map);
        }

        float? rootSize = args["zoom"] is JsonValue zoom
            ? zoom.GetValueKind() == JsonValueKind.Number ? (float)Arguments.Number(zoom) : Named(zoom.ToString())
            : null;
        Look(cell, rootSize);
        return CommandResult.Done(new JsonObject { ["cell"] = Arguments.Json(cell), ["rootSize"] = Find.CameraDriver.RootSize });
    }

    /// <summary>Without a root size, the zoom stays as it is.</summary>
    public static void Look(IntVec3 cell, float? rootSize)
    {
        var driver = Find.CameraDriver;
        driver.JumpToCurrentMapLoc(cell);
        if (rootSize is { } size)
        {
            driver.SetRootSize(size);
        }
    }

    public static void Select(Thing thing)
    {
        Find.Selector.ClearSelection();
        Find.Selector.Select(thing, playSound: false);
    }

    private static float Named(string zoom) => zoom.ToLowerInvariant() switch
    {
        "close" => 11f,
        "middle" => 24f,
        "far" => 45f,
        _ => throw new HarnessException($"Unknown zoom '{zoom}'. Use close, middle, far or a number."),
    };
}
