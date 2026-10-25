namespace ArchotechHarness.Cli;

/// <summary>Where the game, the player's data and the harness state live.</summary>
/// <remarks>Set RIMWORLD_DIR to override the game folder and ARCHO_HOME to override the harness repo root.</remarks>
public sealed class HarnessPaths
{
    public const string DefaultGameDirectory = @"D:\Program Files (x86)\Steam\steamapps\common\RimWorld";

    private HarnessPaths(string gameDirectory, string playerDataDirectory, string home)
    {
        GameDirectory = gameDirectory;
        PlayerDataDirectory = playerDataDirectory;
        Home = home;
    }

    public string GameDirectory { get; }

    public string PlayerDataDirectory { get; }

    public string Home { get; }

    public string GameExecutable => Path.Combine(GameDirectory, "RimWorld.exe");

    public string ModsDirectory => Path.Combine(GameDirectory, "Mods");

    public string HarnessModDirectory => Path.Combine(Home, "mod");

    public string WindowIcon => Path.Combine(Home, "art", "ArchotechHarness.ico");

    public string WorkshopDirectory => Path.GetFullPath(Path.Combine(GameDirectory, "..", "..", "workshop", "content", "294100"));

    public string StateDirectory => Path.Combine(Home, ".harness");

    /// <summary>The isolated save data folder passed to the game with -savedatafolder.</summary>
    public string ProfileDirectory => Path.Combine(StateDirectory, "profile");

    public string SessionDirectory => Path.Combine(StateDirectory, "session");

    public string SessionFile => Path.Combine(SessionDirectory, "session.json");

    public string BackupDirectory => Path.Combine(SessionDirectory, "backup");

    public string RunsDirectory => Path.Combine(StateDirectory, "runs");

    public string PlayerLog => Path.Combine(PlayerDataDirectory, "Logs", "Player.log");

    public string GameVersion
    {
        get
        {
            var versionFile = Path.Combine(GameDirectory, "Version.txt");
            return File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "1.7";
        }
    }

    public static HarnessPaths Detect()
    {
        var game = Environment.GetEnvironmentVariable("RIMWORLD_DIR") is { Length: > 0 } fromEnv ? fromEnv : DefaultGameDirectory;
        var localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
        var playerData = Path.Combine(localLow, "Ludeon Studios", "RimWorld by Ludeon Studios");
        var home = Environment.GetEnvironmentVariable("ARCHO_HOME") is { Length: > 0 } homeFromEnv ? homeFromEnv : FindHome();
        return new HarnessPaths(game, playerData, Path.GetFullPath(home));
    }

    private static string FindHome()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "mod", "About", "About.xml"))
                && File.Exists(Path.Combine(directory.FullName, "ArchotechHarness.sln")))
            {
                return directory.FullName;
            }
        }

        throw new ScenarioException("Cannot find the harness repo root above the archo binary; set ARCHO_HOME.");
    }
}
