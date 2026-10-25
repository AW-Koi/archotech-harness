namespace DevHarness.Cli;

/// <summary>The player files a restore put back, and whether every hash now matches the backup.</summary>
public sealed record RestoreReport(IReadOnlyList<string> Restored, bool Verified);
