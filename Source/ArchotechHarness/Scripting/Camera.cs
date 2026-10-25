using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Points the camera. Zoom is a root size from 11 (closest) to 60.</summary>
public static class Camera
{
    public const float Close = 11f;

    public static void Look(IntVec3 cell, float zoom = 13f) =>
        Script.CommandNow("camera", new JsonObject { ["cell"] = new JsonArray(cell.x, cell.z), ["zoom"] = zoom });

    /// <summary>Centres on a room, zoomed so the whole room fits.</summary>
    public static void Frame(HarnessRoom room) =>
        Look(room.Center, System.Math.Max(Close, System.Math.Max(room.Bounds.Width, room.Bounds.Height) * 0.75f + 4f));

    public static void Follow(Pawn pawn, float zoom = 13f, bool select = true) =>
        Script.CommandNow("camera", new JsonObject { ["pawn"] = pawn.ThingID, ["zoom"] = zoom, ["select"] = select });
}
