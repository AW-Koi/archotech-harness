using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Runs many scenarios with one game boot per mod and DLC set, and a fresh colony before each scenario.</summary>
/// <remarks>Prints one JSON line per scenario and a summary line, and writes run.json and report.html.</remarks>
public sealed class ScenarioBatch(SessionManager sessions, TextWriter output, TextWriter progress)
{
    public IReadOnlyList<ScenarioOutcome> Run(BatchOptions options)
    {
        var filter = new ScenarioFilter(options.Filters);
        var scenarios = ScenarioDiscovery.Discover(options.Paths).Where(s => filter.Matches(s.TypeName)).ToList();
        if (scenarios.Count == 0)
        {
            throw new ScenarioException("No scenarios match.");
        }

        sessions.ThrowIfBusy();
        foreach (var build in scenarios.SelectMany(s => s.Mods).Select(m => m.Build).OfType<BuildCommand>().Distinct())
        {
            sessions.Build(build);
        }

        var assemblies = scenarios.Select(s => s.ProjectFile).Distinct().ToDictionary(
            project => project,
            project => ScenarioProjectBuilder.Build(project, Path.Combine(options.OutputDirectory, "bin", Path.GetFileNameWithoutExtension(project))));

        var outcomes = new List<ScenarioOutcome>();
        foreach (var group in ScenarioGrouping.Group(scenarios))
        {
            progress.WriteLine($"Booting for {group.Name}: {group.Scenarios.Count} scenario(s).");
            outcomes.AddRange(RunGroup(group, assemblies, options));
        }

        RunReport.WriteJson(Path.Combine(options.OutputDirectory, "run.json"), outcomes);
        HtmlReport.Write(Path.Combine(options.OutputDirectory, "report.html"), outcomes);
        output.WriteLine(new JsonObject
        {
            ["scenarios"] = outcomes.Count,
            ["failed"] = outcomes.Count(o => !o.Passed),
            ["report"] = Path.Combine(options.OutputDirectory, "report.html"),
            ["json"] = Path.Combine(options.OutputDirectory, "run.json"),
        }.ToJsonString());
        return outcomes;
    }

    private List<ScenarioOutcome> RunGroup(ScenarioGroup group, Dictionary<string, string> assemblies, BatchOptions options)
    {
        var outcomes = new List<ScenarioOutcome>();
        var session = sessions.Start(new LaunchOptions(group.Dlcs, WithoutBuilds(group), group.Window, options.Port));
        var freshGame = true;
        try
        {
            foreach (var scenario in group.Scenarios)
            {
                var outcome = RunScenario(scenario, group, assemblies[scenario.ProjectFile], options, ref session, ref freshGame);
                outcomes.Add(outcome);
                output.WriteLine(new JsonObject
                {
                    ["scenario"] = scenario.TypeName,
                    ["passed"] = outcome.Passed,
                    ["attempts"] = outcome.Attempts,
                    ["setupSeconds"] = Math.Round(outcome.SetupSeconds, 2),
                    ["runSeconds"] = Math.Round(outcome.RunSeconds, 2),
                    ["error"] = outcome.Error,
                }.ToJsonString());
                output.Flush();
            }
        }
        finally
        {
            var restore = sessions.Stop();
            progress.WriteLine($"Closed the {group.Name} game; player files verified: {restore?.Verified}.");
        }

        return outcomes;
    }

    private ScenarioOutcome RunScenario(ScenarioClass scenario, ScenarioGroup group, string assembly, BatchOptions options,
        ref SessionState session, ref bool freshGame)
    {
        var simpleName = scenario.TypeName[(scenario.TypeName.LastIndexOf('.') + 1)..];
        var outputDirectory = Path.Combine(options.OutputDirectory, simpleName);
        ScenarioOutcome? outcome = null;
        for (var attempt = 1; attempt <= options.Retries + 1; attempt++)
        {
            double setup = 0;
            JsonObject report;
            try
            {
                session = EnsureRunning(session, group, options.Port);
                if (!freshGame)
                {
                    setup = ScenarioInvoker.NewGame(new HarnessClient(session.Port));
                }

                freshGame = false;
                var timer = Stopwatch.StartNew();
                report = ScenarioInvoker.Run(new HarnessClient(session.Port), assembly, scenario.TypeName, outputDirectory);
                report["seconds"] ??= Math.Round(timer.Elapsed.TotalSeconds, 2);
            }
            catch (ScenarioException e)
            {
                report = new JsonObject { ["passed"] = false, ["error"] = e.Message };
            }

            var passed = report["passed"]?.GetValue<bool>() == true;
            outcome = new ScenarioOutcome(scenario, group.Name, passed, attempt, setup,
                report["seconds"]?.GetValue<double>() ?? 0, Error(report), report, outputDirectory);
            if (passed)
            {
                break;
            }

            progress.WriteLine($"{scenario.TypeName} failed on attempt {attempt}: {outcome.Error}");
        }

        return outcome!;
    }

    /// <summary>Boots the group's game again if the previous scenario crashed it.</summary>
    private SessionState EnsureRunning(SessionState session, ScenarioGroup group, int port)
    {
        if (new HarnessClient(session.Port).TryPing())
        {
            return session;
        }

        progress.WriteLine("The game is gone; booting a new one.");
        sessions.Stop();
        return sessions.Start(new LaunchOptions(group.Dlcs, WithoutBuilds(group), group.Window, port));
    }

    private static IReadOnlyList<ScenarioMod> WithoutBuilds(ScenarioGroup group) =>
        group.Mods.Select(m => m with { Build = null }).ToList();

    private static string? Error(JsonObject report)
    {
        if (report["error"] is JsonValue error)
        {
            return error.ToString();
        }

        var failed = report["checks"]?.AsArray().Where(c => c?["passed"]?.GetValue<bool>() == false).Select(c => c!["check"]!.ToString()).ToList();
        return failed is { Count: > 0 } ? "Failed check: " + string.Join("; ", failed) : null;
    }
}
