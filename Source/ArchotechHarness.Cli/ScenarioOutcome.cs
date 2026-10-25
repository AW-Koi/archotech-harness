using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>How one scenario went: the final attempt's report, the fresh-game setup time and the run time.</summary>
/// <remarks>A skipped scenario never ran; SkipReason says why, and it counts as neither passed nor failed.</remarks>
public sealed record ScenarioOutcome(
    ScenarioClass Scenario,
    string Group,
    bool Passed,
    int Attempts,
    double SetupSeconds,
    double RunSeconds,
    string? Error,
    JsonObject? Report,
    string OutputDirectory,
    string? SkipReason = null)
{
    public bool Skipped => SkipReason != null;

    public bool Failed => !Passed && !Skipped;

    public static ScenarioOutcome Skip(ScenarioClass scenario, string group, string reason, string outputDirectory) =>
        new(scenario, group, false, 0, 0, 0, null, null, outputDirectory, reason);
}
