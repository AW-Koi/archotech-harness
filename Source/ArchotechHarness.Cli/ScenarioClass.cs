namespace ArchotechHarness.Cli;

/// <summary>A HarnessScenario class found in a source file, with its project and the profile its attributes ask for.</summary>
/// <remarks>RequiredMods holds the package ids of installed mods it loads besides the mods under test. Biome is a BiomeDef name, or null for any tile.</remarks>
public sealed record ScenarioClass(
    string TypeName,
    string SourceFile,
    string ProjectFile,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<string> Dlcs,
    WindowSize Window,
    IReadOnlyList<string> RequiredMods,
    string? Biome = null);
