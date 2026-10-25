namespace ArchotechHarness.Cli;

/// <summary>Where the game, the player's data and the harness state live.</summary>
/// <remarks>The game folder is found on first use (RIMWORLD_DIR, then Steam); ARCHO_HOME overrides the harness repo root.</remarks>
public sealed class HarnessPaths
{
    private readonly Lazy<string> _gameDirectory = new(RimWorldLocator.Find);

    private HarnessPaths(string playerDataDirectory, string home)
    {
        PlayerDataDirectory = playerDataDirectory;
        Home = home;
    }

    public string GameDirectory => _gameDirectory.Value;

    public string PlayerDataDirectory { get; }

    public string Home { get; }

    public string GameExecutable => Path.Combine(GameDirectory, "RimWorld.exe");

    public string ModsDirectory => Path.Combine(GameDirectory, "Mods");

    public string HarnessModDirectory => Path.Combine(Home, "mod");

    public string WindowIcon => Path.Combine(Home, "art", "ArchotechHarness.ico");

    public string WorkshopDirectory => Path.GetFullPath(Path.Combine(GameDirectory, "..", "..", "workshop", "content", RimWorldLocator.SteamAppId));

    public string StateDirectory => Path.Combine(Home, ".harness");

    /// <summary>State every archo checkout on this machine shares: live sessions, the player-file backup, junctions and the lock.</summary>
    public string MachineStateDirectory { get; } =
        Environment.GetEnvironmentVariable("ARCHO_STATE") is { Length: > 0 } fromEnv
            ? fromEnv
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArchotechHarness");

    public string SessionsDirectory => Path.Combine(MachineStateDirectory, "sessions");

    public string BackupDirectory => Path.Combine(MachineStateDirectory, "backup");

    public string JunctionsFile => Path.Combine(MachineStateDirectory, "junctions.json");

    public string LockFile => Path.Combine(MachineStateDirectory, "state.lock");

    public string RunsDirectory => Path.Combine(StateDirectory, "runs");

    public string TestRunsDirectory => Path.Combine(StateDirectory, "test-runs");

    public string PlayerLog => Path.Combine(PlayerDataDirectory, "Logs", "Player.log");

    /// <summary>A session's registry file; ids carry the checkout, so two checkouts never share one.</summary>
    public string SessionFile(string id) => Path.Combine(SessionsDirectory, $"{CheckoutKey}-{id}.json");

    public string CheckoutKey => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(Home.ToLowerInvariant())))[..12];

    /// <summary>The isolated save data folder a session passes to the game with -savedatafolder.</summary>
    public string ProfileDirectory(string id) =>
        id == SessionManager.MainSessionId ? Path.Combine(StateDirectory, "profile") : Path.Combine(StateDirectory, "profiles", id);

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
        var localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
        var playerData = Path.Combine(localLow, "Ludeon Studios", "RimWorld by Ludeon Studios");
        var home = Environment.GetEnvironmentVariable("ARCHO_HOME") is { Length: > 0 } homeFromEnv ? homeFromEnv : FindHome();
        return new HarnessPaths(playerData, Path.GetFullPath(home));
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
