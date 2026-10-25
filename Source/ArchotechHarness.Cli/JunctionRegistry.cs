using System.Text.Json;

namespace ArchotechHarness.Cli;

/// <summary>The Mods folder junctions archo created, shared by every session so the last one can remove them.</summary>
public static class JunctionRegistry
{
    public static List<string> Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? [] : [];

    public static void Add(string path, string link)
    {
        var links = Load(path);
        if (links.Contains(link, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        links.Add(link);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(links));
    }
}
