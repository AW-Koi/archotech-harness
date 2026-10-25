namespace ArchotechHarness.Cli;

/// <summary>Finds ffmpeg: the FFMPEG environment variable, the PATH, then WinGet's links and package folders.</summary>
public static class FfmpegLocator
{
    public static string? Find()
    {
        if (Environment.GetEnvironmentVariable("FFMPEG") is { Length: > 0 } configured && File.Exists(configured))
        {
            return configured;
        }

        var onPath = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory.Trim('"'), "ffmpeg.exe"))
            .FirstOrDefault(File.Exists);
        if (onPath != null)
        {
            return onPath;
        }

        var winGet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet");
        var link = Path.Combine(winGet, "Links", "ffmpeg.exe");
        if (File.Exists(link))
        {
            return link;
        }

        var packages = Path.Combine(winGet, "Packages");
        return Directory.Exists(packages)
            ? Directory.EnumerateDirectories(packages, "*FFmpeg*")
                .SelectMany(package => Directory.EnumerateFiles(package, "ffmpeg.exe", SearchOption.AllDirectories))
                .FirstOrDefault()
            : null;
    }
}
