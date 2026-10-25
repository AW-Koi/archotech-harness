using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>How one scenario went: the final attempt's report, the fresh-game setup time and the run time.</summary>
public sealed record ScenarioOutcome(
    ScenarioClass Scenario,
    string Group,
    bool Passed,
    int Attempts,
    double SetupSeconds,
    double RunSeconds,
    string? Error,
    JsonObject? Report,
    string OutputDirectory);
