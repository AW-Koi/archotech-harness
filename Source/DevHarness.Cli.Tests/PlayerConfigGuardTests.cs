namespace DevHarness.Cli.Tests;

public sealed class PlayerConfigGuardTests : IDisposable
{
    private const string OriginalModsConfig = "<ModsConfigData><activeMods><li>player.mod</li></activeMods></ModsConfigData>";
    private const string OriginalPrefs = "<PrefsData><devMode>False</devMode></PrefsData>";
    private const string OriginalLog = "last session log of the player";
    private const string SessionLog = "harness session log";

    private readonly TempDirectory _temp = new();
    private readonly string _playerData;
    private readonly string _backup;

    public PlayerConfigGuardTests()
    {
        _playerData = _temp.Combine("player");
        _backup = _temp.Combine("backup");
        _temp.WriteFile("player/Config/ModsConfig.xml", OriginalModsConfig);
        _temp.WriteFile("player/Config/Prefs.xml", OriginalPrefs);
        _temp.WriteFile("player/Logs/Player.log", OriginalLog);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Restore_AfterTheGameRewroteConfig_PutsBackOriginalBytes()
    {
        var guard = new PlayerConfigGuard(_playerData, _backup);
        guard.Backup();
        File.WriteAllText(PlayerFile("Config", "ModsConfig.xml"), "<ModsConfigData />");
        File.WriteAllText(PlayerFile("Config", "Prefs.xml"), "<PrefsData />");

        var report = guard.Restore();

        Assert.True(report.Verified);
        Assert.Equal(OriginalModsConfig, File.ReadAllText(PlayerFile("Config", "ModsConfig.xml")));
        Assert.Equal(OriginalPrefs, File.ReadAllText(PlayerFile("Config", "Prefs.xml")));
    }

    [Fact]
    public void Restore_AfterASessionLogRotation_KeepsTheSessionLogAndRestoresThePlayerLogs()
    {
        var guard = new PlayerConfigGuard(_playerData, _backup);
        guard.Backup();
        File.Move(PlayerFile("Logs", "Player.log"), PlayerFile("Logs", "Player-prev.log"));
        File.WriteAllText(PlayerFile("Logs", "Player.log"), SessionLog);

        guard.Restore();

        Assert.Equal(OriginalLog, File.ReadAllText(PlayerFile("Logs", "Player.log")));
        Assert.False(File.Exists(PlayerFile("Logs", "Player-prev.log")));
        Assert.Equal(SessionLog, File.ReadAllText(Path.Combine(_backup, "session-Player.log")));
    }

    [Fact]
    public void Restore_FromANewGuardInstance_UsesTheManifestOnDisk()
    {
        new PlayerConfigGuard(_playerData, _backup).Backup();
        File.WriteAllText(PlayerFile("Config", "Prefs.xml"), "<PrefsData />");

        var report = new PlayerConfigGuard(_playerData, _backup).Restore();

        Assert.Equal(["Config/Prefs.xml"], report.Restored);
        Assert.Equal(OriginalPrefs, File.ReadAllText(PlayerFile("Config", "Prefs.xml")));
    }

    [Fact]
    public void Restore_WhenNothingChanged_RestoresNothing()
    {
        var guard = new PlayerConfigGuard(_playerData, _backup);
        guard.Backup();

        var report = guard.Restore();

        Assert.Empty(report.Restored);
        Assert.True(report.Verified);
    }

    [Fact]
    public void Restore_WithoutBackup_Throws()
    {
        var guard = new PlayerConfigGuard(_playerData, _backup);

        Assert.Throws<InvalidOperationException>(() => guard.Restore());
    }

    private string PlayerFile(string folder, string name) => Path.Combine(_playerData, folder, name);
}
