using System.Diagnostics;
using System.Globalization;

namespace ArchotechHarness.Cli;

/// <summary>Encodes a recording's frames into an mp4 with ffmpeg, then deletes the frames.</summary>
public static class VideoEncoder
{
    /// <summary>Returns the video path, or throws with ffmpeg's message; keeps the frames when encoding fails.</summary>
    public static string Encode(string framesDirectory, double framesPerSecond, string videoPath)
    {
        if (FrameSequence.Renumber(framesDirectory) == 0)
        {
            throw new ScenarioException("The recording has no frames.");
        }

        var ffmpeg = FfmpegLocator.Find()
            ?? throw new ScenarioException("ffmpeg not found; set FFMPEG to ffmpeg.exe. The frames are kept in " + framesDirectory);
        var start = new ProcessStartInfo(ffmpeg)
        {
            ArgumentList =
            {
                "-y", "-loglevel", "error",
                "-framerate", framesPerSecond.ToString(CultureInfo.InvariantCulture),
                "-i", Path.Combine(framesDirectory, FrameSequence.Pattern),
                "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2",
                "-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
                videoPath,
            },
            UseShellExecute = false,
            RedirectStandardError = true,
        };
        using (var process = Process.Start(start)!)
        {
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new ScenarioException("ffmpeg failed: " + error.Trim());
            }
        }

        Directory.Delete(framesDirectory, recursive: true);
        return videoPath;
    }
}
