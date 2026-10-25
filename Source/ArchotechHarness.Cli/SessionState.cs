using System.Text.Json;

namespace ArchotechHarness.Cli;

/// <summary>A running session and the checkout that started it, saved so its watchdog or a later command can clean it up.</summary>
public sealed record SessionState(string Id, string Home, int GameProcessId, int Port, DateTime StartedUtc)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static SessionState? Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<SessionState>(File.ReadAllText(path), Options) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
    }
}
