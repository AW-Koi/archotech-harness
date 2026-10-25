namespace ArchotechHarness.Cli;

/// <summary>Scenarios that share a mod and DLC set, and so one game boot.</summary>
/// <remarks>RequiredMods includes their dependencies. A group with a SkipReason never boots.</remarks>
public sealed record ScenarioGroup(
    string Name,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<ModMetadata> RequiredMods,
    IReadOnlyList<string> Dlcs,
    WindowSize Window,
    IReadOnlyList<ScenarioClass> Scenarios,
    string? SkipReason);
