using System.Diagnostics;
using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Builds a scenarios project, runs one HarnessScenario class in the game and prints the report.</summary>
/// <remarks>Each run builds under a unique assembly name, so a kept-open game can load a new version.</remarks>
public sealed class ScenarioClassRunner(SessionManager sessions, TextWriter output)
{
    private const double ScenarioTimeoutSeconds = 1800;

    public bool Run(string sourceFile, string runDirectory, bool keepOpen, int port)
    {
        var scenario = ScenarioClassReader.Read(sourceFile);
        var session = sessions.Current;
        if (session == null)
        {
            foreach (var mod in scenario.Mods.Where(m => m.Build != null))
            {
                sessions.Build(mod.Build!);
            }
        }

        var assemblyName = "Scenarios_" + Guid.NewGuid().ToString("N");
        var binDirectory = Path.Combine(runDirectory, "bin");
        var errors = BuildProject(scenario.ProjectFile, assemblyName, binDirectory);
        if (errors != null)
        {
            output.WriteLine(new JsonObject
            {
                ["passed"] = false,
                ["buildErrors"] = new JsonArray(errors.Select(e => (JsonNode)e).ToArray()),
            }.ToJsonString());
            return false;
        }

        session ??= sessions.Start(new LaunchOptions(
            scenario.Dlcs, scenario.Mods.Select(m => m with { Build = null }).ToList(), scenario.Window, port));
        var response = new HarnessClient(session.Port).Send("runScenario", new JsonObject
        {
            ["assembly"] = Path.Combine(binDirectory, assemblyName + ".dll"),
            ["entryType"] = scenario.TypeName,
            ["outputDirectory"] = runDirectory,
            ["timeoutSeconds"] = ScenarioTimeoutSeconds,
        });

        var report = response["ok"]?.GetValue<bool>() == true
            ? response["result"]!.AsObject()
            : new JsonObject { ["passed"] = false, ["error"] = response["error"]?.DeepClone() };
        report["scenario"] = scenario.TypeName;
        report["runDirectory"] = runDirectory;
        if (!keepOpen)
        {
            var restore = sessions.Stop();
            report["playerFilesVerified"] = restore?.Verified;
        }

        File.WriteAllText(Path.Combine(runDirectory, "report.json"), report.ToJsonString(new() { WriteIndented = true }));
        output.WriteLine(report.ToJsonString());
        return report["passed"]?.GetValue<bool>() == true;
    }

    /// <summary>Returns null on success, otherwise the compiler errors.</summary>
    private static IReadOnlyList<string>? BuildProject(string project, string assemblyName, string binDirectory)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { "build", project, "--nologo", "-v", "q", "-clp:NoSummary" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        start.Environment["RWH_SCENARIO_ASSEMBLY"] = assemblyName;
        start.Environment["RWH_SCENARIO_OUTDIR"] = binDirectory;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return null;
        }

        var lines = (stdout.Result + stderr.Result).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var errors = lines.Where(l => l.Contains(" error ")).Distinct().ToList();
        return errors.Count > 0 ? errors : lines;
    }
}
