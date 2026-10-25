namespace ArchotechHarness.Cli;

/// <summary>What a session loads, its window, its harness port (0 picks a free one) and whether the window is hidden.</summary>
/// <remarks>Mods are the mods under test; RequiredMods are installed mods found by package id, dependencies included.</remarks>
public sealed record LaunchOptions(
    IReadOnlyList<string> Dlcs,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<ModMetadata> RequiredMods,
    WindowSize Window,
    int Port,
    bool Hidden = false);
