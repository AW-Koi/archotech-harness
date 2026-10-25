using System.IO;
using System.Runtime.CompilerServices;

namespace ArchotechHarness.Scripting;

/// <summary>Presets that add finishing touches to a room on free floor; finish with Build().</summary>
public static class Decorate
{
    /// <summary>Choose plant pots, lamps or art; nothing goes on doorways, interaction cells or bedsides.</summary>
    public static DecorationBuilder Room(HarnessRoom room, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(room, $"{Path.GetFileName(file)}:{line}");
}
