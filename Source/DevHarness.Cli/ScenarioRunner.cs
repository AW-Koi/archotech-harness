using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Runs scenario steps against a session and prints one JSON line per step.</summary>
/// <remarks>After a failed step only steps marked "always" run. Relative "path" arguments land in the run folder.</remarks>
public sealed class ScenarioRunner(SessionManager sessions, TextWriter output)
{
    public const string LocalCaptureCommand = "windowCapture";

    public bool Run(Scenario scenario, string runDirectory, bool keepOpen, int port)
    {
        Directory.CreateDirectory(runDirectory);
        var session = sessions.Current
            ?? sessions.Start(new LaunchOptions(scenario.Dlcs, scenario.Mods, scenario.Window, port));
        var client = new HarnessClient(session.Port);
        var variables = new Dictionary<string, JsonNode?> { ["run"] = new JsonObject { ["dir"] = runDirectory } };
        var steps = new JsonArray();
        var failed = false;

        for (var index = 0; index < scenario.Steps.Count; index++)
        {
            var step = scenario.Steps[index];
            var line = new JsonObject { ["step"] = index + 1, ["cmd"] = step.Command };
            if (failed && !step.Always)
            {
                line["skipped"] = true;
                Emit(steps, line);
                continue;
            }

            var timer = Stopwatch.StartNew();
            var (result, error) = Execute(step, client, session, variables, runDirectory);
            try
            {
                error ??= StepCheck.Failure(result, step.Expectation);
            }
            catch (JsonException e)
            {
                error = e.Message;
            }
            if (error == null && step.SaveAs != null)
            {
                variables[step.SaveAs] = result?.DeepClone();
            }

            line["ok"] = error == null;
            line["seconds"] = Math.Round(timer.Elapsed.TotalSeconds, 2);
            line["result"] = result?.DeepClone();
            if (error != null)
            {
                line["error"] = error;
                line["optional"] = step.Optional;
                failed |= !step.Optional;
            }

            Emit(steps, line);
        }

        var summary = new JsonObject
        {
            ["scenario"] = scenario.Name,
            ["passed"] = !failed,
            ["runDirectory"] = runDirectory,
        };
        if (scenario.QuitWhenDone && !keepOpen)
        {
            var report = sessions.Stop();
            summary["playerFilesRestored"] = report == null ? null : new JsonArray(report.Restored.Select(r => (JsonNode)r).ToArray());
            summary["playerFilesVerified"] = report?.Verified;
        }

        File.WriteAllText(Path.Combine(runDirectory, "results.json"),
            new JsonObject { ["summary"] = summary.DeepClone(), ["steps"] = steps }.ToJsonString(new() { WriteIndented = true }));
        output.WriteLine(summary.ToJsonString());
        return !failed;
    }

    private static (JsonNode? Result, string? Error) Execute(
        ScenarioStep step, HarnessClient client, SessionState session, Dictionary<string, JsonNode?> variables, string runDirectory)
    {
        try
        {
            var arguments = (JsonObject)VariableSubstitution.Apply(step.Arguments, variables)!;
            OutputPaths.Resolve(step.Command, arguments, runDirectory);
            if (step.Command == LocalCaptureCommand)
            {
                var path = arguments["path"]?.ToString() ?? throw new ScenarioException("windowCapture needs a \"path\".");
                var (width, height) = WindowCapture.Save(session.GameProcessId, path);
                return (new JsonObject { ["path"] = path, ["width"] = width, ["height"] = height }, null);
            }

            var response = client.Send(step.Command, arguments);
            return response["ok"]?.GetValue<bool>() == true
                ? (response["result"], null)
                : (null, response["error"]?.ToString() ?? "unknown error");
        }
        catch (Exception e) when (e is ScenarioException or IOException or SocketException)
        {
            return (null, e.Message);
        }
    }

    private void Emit(JsonArray steps, JsonObject line)
    {
        output.WriteLine(line.ToJsonString());
        output.Flush();
        steps.Add(line);
    }
}
