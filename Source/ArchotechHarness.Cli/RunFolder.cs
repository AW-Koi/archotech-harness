namespace ArchotechHarness.Cli;

/// <summary>One run's output folder, with its creation time and the bytes it holds.</summary>
public sealed record RunFolder(string Path, DateTime CreatedUtc, long Bytes)
{
    public static RunFolder Measure(string path) => new(
        path,
        Directory.GetCreationTimeUtc(path),
        new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length));
}
