using System.Security.Cryptography;
using System.Text.Json;

namespace ArchotechHarness.Cli;

/// <summary>Backs up the player's config and logs, and restores them byte for byte.</summary>
/// <remarks>The manifest on disk lets a later process restore after a crash or a kill.</remarks>
public sealed class PlayerConfigGuard(string playerDataDirectory, string backupDirectory)
{
    public const string SessionLogFileName = "session-Player.log";

    private const string ManifestFileName = "manifest.json";

    private static readonly string[] GuardedFiles =
    [
        "Config/ModsConfig.xml",
        "Config/Prefs.xml",
        "Logs/Player.log",
        "Logs/Player-prev.log",
    ];

    public bool HasBackup => File.Exists(ManifestPath);

    private string ManifestPath => Path.Combine(backupDirectory, ManifestFileName);

    public void Backup()
    {
        Directory.CreateDirectory(backupDirectory);
        var manifest = new Dictionary<string, string?>();
        foreach (var relative in GuardedFiles)
        {
            var source = PlayerPath(relative);
            if (File.Exists(source))
            {
                File.Copy(source, BackupPath(relative), overwrite: true);
                manifest[relative] = Hash(BackupPath(relative));
            }
            else
            {
                manifest[relative] = null;
            }
        }

        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest));
    }

    public RestoreReport Restore()
    {
        if (!HasBackup)
        {
            throw new InvalidOperationException($"No backup manifest at {ManifestPath}.");
        }

        var manifest = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(ManifestPath))!;
        PreserveSessionLog(manifest);

        var restored = new List<string>();
        foreach (var (relative, originalHash) in manifest)
        {
            var target = PlayerPath(relative);
            var currentHash = File.Exists(target) ? Hash(target) : null;
            if (currentHash == originalHash)
            {
                continue;
            }

            if (originalHash == null)
            {
                File.Delete(target);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(BackupPath(relative), target, overwrite: true);
            }

            restored.Add(relative);
        }

        var verified = manifest.All(entry =>
            (File.Exists(PlayerPath(entry.Key)) ? Hash(PlayerPath(entry.Key)) : null) == entry.Value);
        return new RestoreReport(restored, verified);
    }

    private void PreserveSessionLog(Dictionary<string, string?> manifest)
    {
        var log = PlayerPath("Logs/Player.log");
        if (File.Exists(log) && Hash(log) != manifest.GetValueOrDefault("Logs/Player.log"))
        {
            File.Copy(log, Path.Combine(backupDirectory, SessionLogFileName), overwrite: true);
        }
    }

    private string PlayerPath(string relative) => Path.Combine(playerDataDirectory, relative.Replace('/', Path.DirectorySeparatorChar));

    private string BackupPath(string relative) => Path.Combine(backupDirectory, relative.Replace('/', '_'));

    private static string Hash(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
