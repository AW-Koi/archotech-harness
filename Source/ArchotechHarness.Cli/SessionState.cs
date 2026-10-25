using System.Text.Json;

namespace ArchotechHarness.Cli;

/// <summary>The running session, saved to disk so a watchdog or a later command can clean it up.</summary>
public sealed record SessionState(int GameProcessId, int Port, DateTime StartedUtc, IReadOnlyList<string> CreatedJunctions)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static SessionState? Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<SessionState>(File.ReadAllText(path), Options) : null;

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
    }
}
