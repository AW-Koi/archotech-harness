using System.Text.Json;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Commands;

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
                Find.Selector.ClearSelection();
                Find.Selector.Select(pawn, playSound: false);
            }
        }
        else
        {
            cell = Arguments.Cell(args, "cell", map);
        }

        var driver = Find.CameraDriver;
        driver.JumpToCurrentMapLoc(cell);
        if (args["zoom"] is JsonValue zoom)
        {
            driver.SetRootSize(zoom.GetValueKind() == JsonValueKind.Number ? (float)Arguments.Number(zoom) : Named(zoom.ToString()));
        }

        return CommandResult.Done(new JsonObject { ["cell"] = Arguments.Json(cell), ["rootSize"] = driver.RootSize });
    }

    private static float Named(string zoom) => zoom.ToLowerInvariant() switch
    {
        "close" => 11f,
        "middle" => 24f,
        "far" => 45f,
        _ => throw new HarnessException($"Unknown zoom '{zoom}'. Use close, middle, far or a number."),
    };
}
