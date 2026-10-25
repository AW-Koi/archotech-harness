using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Builds a scenarios project, runs one HarnessScenario class in the game and prints the report.</summary>
/// <remarks>It boots its own session, or with attach uses the running one and leaves it open.</remarks>
public sealed class ScenarioClassRunner(SessionManager sessions, TextWriter output)
{
    public bool Run(string sourceFile, string runDirectory, bool keepOpen, bool attach, int port, double videoFps = 0)
    {
        var scenario = ScenarioClassReader.Read(sourceFile);
        var session = attach ? sessions.Current : null;
        var startsSession = session == null;
        if (startsSession)
        {
            sessions.ThrowIfBusy();
            foreach (var mod in scenario.Mods.Where(m => m.Build != null))
            {
                sessions.Build(mod.Build!);
            }
        }

        string assembly;
        try
        {
            assembly = ScenarioProjectBuilder.Build(scenario.ProjectFile, Path.Combine(runDirectory, "bin"));
        }
        catch (ScenarioBuildException e)
        {
            output.WriteLine(new JsonObject
            {
                ["passed"] = false,
                ["buildErrors"] = new JsonArray(e.Errors.Select(l => (JsonNode)l).ToArray()),
            }.ToJsonString());
            return false;
        }

        session ??= sessions.Start(new LaunchOptions(
            scenario.Dlcs, scenario.Mods.Select(m => m with { Build = null }).ToList(), scenario.Window, port));
        var report = ScenarioInvoker.Run(new HarnessClient(session.Port), assembly, scenario.TypeName, runDirectory, videoFps);
        report["scenario"] = scenario.TypeName;
        report["runDirectory"] = runDirectory;
        if (startsSession && !keepOpen)
        {
            var restore = sessions.Stop();
            report["playerFilesVerified"] = restore?.Verified;
        }

        File.WriteAllText(Path.Combine(runDirectory, "report.json"), report.ToJsonString(new() { WriteIndented = true }));
        output.WriteLine(report.ToJsonString());
        return report["passed"]?.GetValue<bool>() == true;
    }
}
