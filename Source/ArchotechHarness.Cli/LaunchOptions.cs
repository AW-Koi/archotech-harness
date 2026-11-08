namespace ArchotechHarness.Cli;

/// <summary>What a session loads, its window, its harness port (0 picks a free one), whether the window is hidden and whether it plays for filming.</summary>
/// <remarks>Mods are the mods under test; RequiredMods are installed mods found by package id, dependencies included. Biome is the BiomeDef the colony starts on, or null.</remarks>
public sealed record LaunchOptions(
    IReadOnlyList<string> Dlcs,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<ModMetadata> RequiredMods,
    WindowSize Window,
    int Port,
    bool Hidden = false,
    string? Biome = null,
    bool Filming = false);
