namespace ArchotechHarness.Cli;

/// <summary>What archo test runs: where to find scenarios, which to pick, how often to retry, where to write and whether load errors fail it.</summary>
public sealed record BatchOptions(
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> Filters,
    int Retries,
    string OutputDirectory,
    int Port,
    string? Workers = null,
    double VideoFps = 0,
    bool AllowLoadErrors = false)
{
    /// <summary>Shows the scenario debugger in a visible game, one worker only; true without it.</summary>
    public bool DebugPanel { get; init; } = true;
}
