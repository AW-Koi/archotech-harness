namespace DevHarness.Cli;

/// <summary>What a session loads: DLCs, mods under test, window size and the harness port.</summary>
public sealed record LaunchOptions(IReadOnlyList<string> Dlcs, IReadOnlyList<ScenarioMod> Mods, WindowSize Window, int Port)
{
    public const int DefaultPort = 47801;
}
