namespace DevHarness.Cli;

/// <summary>A declarative harness run: the mod profile, the window and the steps to send.</summary>
public sealed record Scenario(
    string Name,
    IReadOnlyList<string> Dlcs,
    IReadOnlyList<ScenarioMod> Mods,
    WindowSize Window,
    IReadOnlyList<ScenarioStep> Steps,
    bool QuitWhenDone);
