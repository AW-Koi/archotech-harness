namespace ArchotechHarness.Cli;

/// <summary>Scenarios that share a mod and DLC set, and so one game boot.</summary>
public sealed record ScenarioGroup(
    string Name,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<string> Dlcs,
    WindowSize Window,
    IReadOnlyList<ScenarioClass> Scenarios);
