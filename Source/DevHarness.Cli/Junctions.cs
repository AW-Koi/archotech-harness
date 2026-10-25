using System.Diagnostics;

namespace DevHarness.Cli;

/// <summary>Creates and removes directory junctions in the game's Mods folder.</summary>
public static class Junctions
{
    public static void Create(string link, string target)
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", "mklink", "/J", link, target },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new ScenarioException($"mklink /J {link} failed: {error.Trim()}");
        }
    }

    /// <summary>Removes the link only; the target folder and its files stay.</summary>
    public static void Remove(string link)
    {
        var info = new DirectoryInfo(link);
        if (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            info.Delete(recursive: false);
        }
    }
}
