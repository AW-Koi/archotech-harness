namespace ArchotechHarness.Cli;

/// <summary>Turns a mod or build path from a scenario declaration into a full path.</summary>
public static class ModPath
{
    public static string Resolve(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path)).TrimEnd(Path.DirectorySeparatorChar);
}
