using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Sends runScenario and newGame to a session and turns the replies into scenario reports.</summary>
public static class ScenarioInvoker
{
    private const double ScenarioTimeoutSeconds = 1800;
    private const double NewGameTimeoutSeconds = 180;

    /// <summary>Runs one scenario class; a harness or connection failure becomes a failed report.</summary>
    public static JsonObject Run(HarnessClient client, string assembly, string typeName, string outputDirectory)
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
            });
            return response["ok"]?.GetValue<bool>() == true
                ? response["result"]!.AsObject()
                : new JsonObject { ["passed"] = false, ["error"] = response["error"]?.DeepClone() };
        }
        catch (Exception e) when (e is IOException or SocketException)
        {
            return new JsonObject { ["passed"] = false, ["error"] = "Lost the game: " + e.Message };
        }
    }

    /// <summary>Replaces the game with a fresh quick-test colony and returns how long that took.</summary>
    public static double NewGame(HarnessClient client)
    {
        var response = client.Send("newGame", new JsonObject { ["timeoutSeconds"] = NewGameTimeoutSeconds });
        return response["ok"]?.GetValue<bool>() == true
            ? response["result"]!["seconds"]!.GetValue<double>()
            : throw new ScenarioException("Could not start a fresh game: " + response["error"]);
    }
}
