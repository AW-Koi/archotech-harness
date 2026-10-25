using ArchotechHarness.Commands;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Points the camera. Zoom is a root size from 11 (closest) to 60.</summary>
public static class Camera
{
    public const float Close = 11f;

    public static void Look(IntVec3 cell, float zoom = 13f) => CameraCommand.Look(cell, zoom);

    /// <summary>Centres on a room, zoomed so the whole room fits.</summary>
    public static void Frame(HarnessRoom room) => Frame(room.Bounds);

    /// <summary>Centres on the area, zoomed so all of it fits.</summary>
    public static void Frame(CellRect area) =>
        Look(area.CenterCell, System.Math.Max(Close, System.Math.Max(area.Width, area.Height) * 0.75f + 4f));

    public static void Follow(Pawn pawn, float zoom = 13f, bool select = true)
    {
        CameraCommand.Look(pawn.Position, zoom);
        if (select)
        {
            CameraCommand.Select(pawn);
        }
    }
}
