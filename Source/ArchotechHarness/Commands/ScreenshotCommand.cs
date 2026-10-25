using System;
using System.IO;
using System.Text.Json.Nodes;
using UnityEngine;

namespace ArchotechHarness.Commands;

/// <summary>Saves the next rendered frame, UI included, as a PNG at an absolute "path".</summary>
/// <remarks>Waylith writes the file a few frames later, so the command waits until the file is complete.</remarks>
public static class ScreenshotCommand
{
    private const double TimeoutSeconds = 20;

    /// <summary>Screenshots asked for and not yet written, so overlays such as the debug panel can stay out of them.</summary>
    public static int Pending { get; private set; }

    public static CommandResult Run(JsonObject args)
    {
        var path = Path.GetFullPath(Arguments.RequiredString(args, "path"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        Pending++;

        var deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
        long lastLength = -1;
        return CommandResult.Pending(() =>
        {
            var length = File.Exists(path) ? new FileInfo(path).Length : 0;
            if (length > 0 && length == lastLength && CanOpen(path))
            {
                Pending--;
                return new JsonObject { ["path"] = path, ["bytes"] = length };
            }

            lastLength = length;
            if (DateTime.UtcNow <= deadline)
            {
                return null;
            }

            Pending--;
            throw new HarnessException($"No screenshot appeared at {path}; try the CLI's window capture instead.");
        });
    }

    private static bool CanOpen(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
