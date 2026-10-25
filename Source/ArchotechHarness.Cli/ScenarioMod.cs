namespace ArchotechHarness.Cli;

/// <summary>A mod under test: its folder (the one holding About/) and an optional build.</summary>
public sealed record ScenarioMod(string Directory, BuildCommand? Build);
