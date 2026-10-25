using System.IO;
using System.Runtime.CompilerServices;

namespace ArchotechHarness.Scripting;

/// <summary>Builds finished structures instantly.</summary>
public static class Build
{
    /// <summary>Starts a walled room; set its Size and finish with Build().</summary>
    public static RoomBuilder Room([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new($"{Path.GetFileName(file)}:{line}");
}
