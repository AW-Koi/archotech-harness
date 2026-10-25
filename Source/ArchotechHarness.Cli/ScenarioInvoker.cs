using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Sends runScenario and newGame to a session and turns the replies into scenario reports.</summary>
public static class ScenarioInvoker
{
    private const double ScenarioTimeoutSeconds = 1800;
    private const double NewGameTimeoutSeconds = 180;

    /// <summary>Runs one scenario class; a harness or connection failure becomes a failed report.</summary>
    /// <remarks>With videoFps above 0 the game records frames, and the report gets "video" or "videoError".</remarks>
    public static JsonObject Run(HarnessClient client, string assembly, string typeName, string outputDirectory, double videoFps = 0) =>
        Run(client, assembly, typeName, outputDirectory, videoFps, debugPanel: false);

    /// <summary>As Run, and with debugPanel the game shows the scenario debugger while the scenario runs, unless it records video.</summary>
    public static JsonObject Run(HarnessClient client, string assembly, string typeName, string outputDirectory, double videoFps, bool debugPanel)
    {
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var response = client.Send("runScenario", new JsonObject
            {
                ["assembly"] = assembly,
                ["entryType"] = typeName,
                ["outputDirectory"] = outputDirectory,
                ["timeoutSeconds"] = ScenarioTimeoutSeconds,
                ["recordFps"] = videoFps,
                ["debugPanel"] = debugPanel,
            });
            var report = response["ok"]?.GetValue<bool>() == true
                ? response["result"]!.AsObject()
                : new JsonObject { ["passed"] = false, ["error"] = response["error"]?.DeepClone() };
            if (videoFps > 0 && report["frames"] is JsonValue frames)
            {
                try
                {
                    report["video"] = VideoEncoder.Encode(frames.ToString(), videoFps, Path.Combine(outputDirectory, "video.mp4"));
                }
                catch (ScenarioException e)
                {
                    report["videoError"] = e.Message;
                }
            }

            return report;
        }
        catch (Exception e) when (e is IOException or SocketException)
        {
            return new JsonObject { ["passed"] = false, ["error"] = "Lost the game: " + e.Message };
        }
    }

    /// <summary>Replaces the game with a fresh quick-test colony, on the biome if given, and returns how long that took.</summary>
    public static double NewGame(HarnessClient client, string? biome = null)
    {
        var response = client.Send("newGame", new JsonObject { ["timeoutSeconds"] = NewGameTimeoutSeconds, ["biome"] = biome });
        return response["ok"]?.GetValue<bool>() == true
            ? response["result"]!["seconds"]!.GetValue<double>()
            : throw new ScenarioException("Could not start a fresh game: " + response["error"]);
    }
}
