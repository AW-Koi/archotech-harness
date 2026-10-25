namespace ArchotechHarness.Cli;

/// <summary>What archo test runs: where to find scenarios, which to pick, how often to retry and where to write.</summary>
public sealed record BatchOptions(
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> Filters,
    int Retries,
    string OutputDirectory,
    int Port,
    string? Workers = null);
