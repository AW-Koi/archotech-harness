namespace ArchotechHarness.Cli;

/// <summary>What a session loads, its window, its harness port (0 picks a free one) and whether the window is hidden.</summary>
public sealed record LaunchOptions(IReadOnlyList<string> Dlcs, IReadOnlyList<ScenarioMod> Mods, WindowSize Window, int Port, bool Hidden = false);
