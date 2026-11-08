namespace ArchotechHarness.Cli;

/// <summary>The RimWorld command line for a session.</summary>
/// <remarks>-WindowSize locks the window, so a filming session leaves it off and Prefs.xml sizes the window.</remarks>
public static class GameArguments
{
    public static IReadOnlyList<string> For(LaunchOptions options, string profile, int port)
    {
        var arguments = new List<string> { "-quicktest", $"\"-savedatafolder={profile}\"" };
        if (!options.Filming)
        {
            arguments.Add($"-WindowSize={options.Window.Width},{options.Window.Height}");
        }

        arguments.AddRange([$"-harnessPort={port}", "-NoCrashDialog", "-DisableLogStackTraces"]);
        if (options.Hidden)
        {
            arguments.AddRange(["-NoWindow", "-NoFileLog"]);
        }

        if (options.Biome != null)
        {
            arguments.Add($"-harnessBiome={options.Biome}");
        }

        return arguments;
    }
}
