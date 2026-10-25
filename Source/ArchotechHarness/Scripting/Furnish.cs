using System.IO;
using System.Runtime.CompilerServices;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Presets that furnish a built room in one go; finish each with Build().</summary>
public static class Furnish
{
    /// <summary>A bed against the wall opposite the door, end tables either side and a dresser, plus any extras.</summary>
    public static BedroomBuilder Bedroom(HarnessRoom room, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(room, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Fills the area, inside the room's floor, with tables and chairs facing them.</summary>
    public static DiningBuilder Dining(HarnessRoom room, CellRect area, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(room, area, $"{Path.GetFileName(file)}:{line}");
}
