using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Resolves the relative file "path" of capture commands against the run folder.</summary>
public static class OutputPaths
{
    private static readonly HashSet<string> FileWritingCommands =
        new(StringComparer.OrdinalIgnoreCase) { "screenshot", ScenarioRunner.LocalCaptureCommand };

    public static void Resolve(string command, JsonObject arguments, string runDirectory)
    {
        if (FileWritingCommands.Contains(command)
            && arguments["path"] is JsonValue path
            && !Path.IsPathRooted(path.ToString()))
        {
            arguments["path"] = Path.Combine(runDirectory, path.ToString());
        }
    }
}
