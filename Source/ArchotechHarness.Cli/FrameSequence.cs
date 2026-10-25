namespace ArchotechHarness.Cli;

/// <summary>Turns the frame_NNNNN.png files a recording left into a gap-free seq_NNNNN.png sequence for ffmpeg.</summary>
/// <remarks>Skips empty files and files still being written.</remarks>
public static class FrameSequence
{
    public const string Pattern = "seq_%05d.png";

    public static int Renumber(string framesDirectory)
    {
        if (!Directory.Exists(framesDirectory))
        {
            return 0;
        }

        var frames = Directory.GetFiles(framesDirectory, "frame_*.png")
            .Order(StringComparer.Ordinal)
            .Where(Complete)
            .ToList();
        for (var i = 0; i < frames.Count; i++)
        {
            File.Move(frames[i], Path.Combine(framesDirectory, $"seq_{i:D5}.png"));
        }

        return frames.Count;
    }

    private static bool Complete(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
