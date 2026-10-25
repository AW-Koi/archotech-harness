using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Runs many scenarios across a pool of games, starting each scenario after a game's first in a fresh colony.</summary>
/// <remarks>Each worker keeps its game while the next scenario needs the same mod and DLC set. With several workers the games run hidden.</remarks>
public sealed class ScenarioBatch(SessionManager sessions, TextWriter output, TextWriter progress)
{
    private readonly object _outputLock = new();

    private RestoreReport? _lastRestore;

    private SessionManager Sessions => sessions;

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

        var queue = new WorkQueue(ScenarioGrouping.Group(scenarios));
        var workers = WorkerCount.Choose(options.Workers, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, Environment.ProcessorCount, scenarios.Count);
        var outcomes = new List<ScenarioOutcome>();
        var timer = Stopwatch.StartNew();
        var threads = Enumerable.Range(1, workers).Select(index => new Thread(() =>
        {
            var id = workers == 1 ? SessionManager.MainSessionId : $"w{index}";
            var results = new Worker(this, id, workers > 1, assemblies, options).Run(queue);
            lock (outcomes)
            {
                outcomes.AddRange(results);
            }
        })).ToList();
        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        var restore = sessions.StopAll() ?? _lastRestore;
        var ordered = scenarios.Select(s => outcomes.First(o => o.Scenario == s)).ToList();
        RunReport.WriteJson(Path.Combine(options.OutputDirectory, "run.json"), ordered);
        HtmlReport.Write(Path.Combine(options.OutputDirectory, "report.html"), ordered);
        Emit(new JsonObject
        {
            ["scenarios"] = ordered.Count,
            ["failed"] = ordered.Count(o => !o.Passed),
            ["workers"] = workers,
            ["seconds"] = Math.Round(timer.Elapsed.TotalSeconds, 1),
            ["playerFilesVerified"] = restore?.Verified,
            ["report"] = Path.Combine(options.OutputDirectory, "report.html"),
            ["json"] = Path.Combine(options.OutputDirectory, "run.json"),
        });
        return ordered;
    }

    /// <summary>Keeps the restore report of whichever session ended last.</summary>
    private void Ended(RestoreReport? report)
    {
        if (report != null)
        {
            lock (_outputLock)
            {
                _lastRestore = report;
            }
        }
    }

    private void Emit(JsonNode line)
    {
        lock (_outputLock)
        {
            output.WriteLine(line.ToJsonString());
            output.Flush();
        }
    }

    private void Progress(string message)
    {
        lock (_outputLock)
        {
            progress.WriteLine(message);
        }
    }

    private static string? Error(JsonObject report)
    {
        if (report["error"] is JsonValue error)
        {
            return error.ToString();
        }

        var failed = report["checks"]?.AsArray().Where(c => c?["passed"]?.GetValue<bool>() == false).Select(c => c!["check"]!.ToString()).ToList();
        return failed is { Count: > 0 } ? "Failed check: " + string.Join("; ", failed) : null;
    }

    /// <summary>Scenarios still to run, handed out so workers keep their games and avoid needless boots.</summary>
    /// <remarks>A worker takes its own group first, then a group no one holds, then a held group with two or more left.</remarks>
    private sealed class WorkQueue(IReadOnlyList<ScenarioGroup> groups)
    {
        private readonly List<(ScenarioGroup Group, ScenarioClass Scenario)> _pending =
            groups.SelectMany(g => g.Scenarios.Select(s => (g, s))).ToList();

        private readonly Dictionary<string, ScenarioGroup> _holders = new();

        public (ScenarioGroup Group, ScenarioClass Scenario)? Next(string worker)
        {
            lock (_pending)
            {
                var mine = _holders.GetValueOrDefault(worker);
                var held = _holders.Where(h => h.Key != worker).Select(h => h.Value).ToHashSet();
                var index = _pending.FindIndex(p => p.Group == mine);
                if (index < 0)
                {
                    index = _pending.FindIndex(p => !held.Contains(p.Group));
                }

                if (index < 0)
                {
                    index = _pending.FindIndex(p => _pending.Count(q => q.Group == p.Group) >= 2);
                }

                if (index < 0)
                {
                    _holders.Remove(worker);
                    return null;
                }

                var next = _pending[index];
                _pending.RemoveAt(index);
                _holders[worker] = next.Group;
                return next;
            }
        }
    }

    /// <summary>One game in the pool: boots for a group, runs scenarios in fresh colonies and reboots when the group changes.</summary>
    private sealed class Worker(ScenarioBatch batch, string id, bool hidden, Dictionary<string, string> assemblies, BatchOptions options)
    {
        private SessionState? _session;
        private ScenarioGroup? _group;
        private bool _freshGame;

        public List<ScenarioOutcome> Run(WorkQueue queue)
        {
            var outcomes = new List<ScenarioOutcome>();
            try
            {
                while (queue.Next(id) is { } next)
                {
                    var outcome = RunWithRetries(next.Group, next.Scenario);
                    outcomes.Add(outcome);
                    batch.Emit(new JsonObject
                    {
                        ["scenario"] = outcome.Scenario.TypeName,
                        ["worker"] = id,
                        ["passed"] = outcome.Passed,
                        ["attempts"] = outcome.Attempts,
                        ["setupSeconds"] = Math.Round(outcome.SetupSeconds, 2),
                        ["runSeconds"] = Math.Round(outcome.RunSeconds, 2),
                        ["error"] = outcome.Error,
                    });
                }
            }
            finally
            {
                batch.Ended(batch.Sessions.Stop(id));
            }

            return outcomes;
        }

        private ScenarioOutcome RunWithRetries(ScenarioGroup group, ScenarioClass scenario)
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
                    var session = EnsureGame(group);
                    if (!_freshGame)
                    {
                        setup = ScenarioInvoker.NewGame(new HarnessClient(session.Port));
                    }

                    _freshGame = false;
                    var timer = Stopwatch.StartNew();
                    report = ScenarioInvoker.Run(new HarnessClient(session.Port), assemblies[scenario.ProjectFile], scenario.TypeName, outputDirectory);
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

                batch.Progress($"{scenario.TypeName} failed on attempt {attempt} ({id}): {outcome.Error}");
            }

            return outcome!;
        }

        /// <summary>Returns a running game for the group, booting one when the group changed or the game is gone.</summary>
        private SessionState EnsureGame(ScenarioGroup group)
        {
            if (_session != null && _group == group && new HarnessClient(_session.Port).TryPing())
            {
                return _session;
            }

            if (_session != null)
            {
                batch.Ended(batch.Sessions.Stop(id));
            }

            batch.Progress($"Worker {id}: booting for {group.Name}.");
            _group = group;
            _session = batch.Sessions.Start(
                new LaunchOptions(group.Dlcs, group.Mods.Select(m => m with { Build = null }).ToList(), group.Window, hidden ? 0 : options.Port, hidden),
                id, checkBusy: false);
            _freshGame = true;
            return _session;
        }
    }
}
