namespace DevHarness.Cli;

/// <summary>A HarnessScenario class found in a source file, with its project and the profile its attributes ask for.</summary>
public sealed record ScenarioClass(
    string TypeName,
    string ProjectFile,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<string> Dlcs,
    WindowSize Window);
