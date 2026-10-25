using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Builds a scenarios project, runs one HarnessScenario class in the game and prints the report.</summary>
/// <remarks>It boots its own session, or with attach uses the running one and leaves it open.</remarks>
public sealed class ScenarioClassRunner(SessionManager sessions, TextWriter output, TextWriter progress)
{
    /// <summary>Shows the scenario debugger in the game while the scenario runs; true without it.</summary>
    public bool DebugPanel { get; init; } = true;

    /// <summary>Returns false when the scenario failed; a scenario skipped for a missing required mod returns true.</summary>
    public bool Run(string sourceFile, string runDirectory, bool keepOpen, bool attach, int port, double videoFps = 0)
    {
        var scenario = ScenarioClassReader.Read(sourceFile);
        var required = sessions.CreateRequiredModResolver().Resolve(scenario.RequiredMods);
        if (required.SkipReason is { } skipReason)
        {
            progress.WriteLine($"Skipped {scenario.TypeName}: {skipReason}.");
            output.WriteLine(new JsonObject
            {
                ["scenario"] = scenario.TypeName,
                ["passed"] = false,
                ["skipped"] = true,
                ["skipReason"] = skipReason,
            }.ToJsonString());
            return true;
        }
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
            scenario.Dlcs.Concat(required.Dlcs).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            scenario.Mods.Select(m => m with { Build = null }).ToList(), required.Mods, scenario.Window, port, Biome: scenario.Biome));
        var report = ScenarioInvoker.Run(new HarnessClient(session.Port), assembly, scenario.TypeName, runDirectory, videoFps, DebugPanel);
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
