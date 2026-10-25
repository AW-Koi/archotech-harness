using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Runs many scenarios across a pool of games, starting each scenario after a game's first in a fresh colony.</summary>
/// <remarks>Each worker keeps its game while the next scenario needs the same mod and DLC set. With several workers the games run hidden.
/// Errors that mods under test log while a group's game loads fail that group's scenarios. Groups missing a required mod are skipped.</remarks>
public sealed class ScenarioBatch(SessionManager sessions, TextWriter output, TextWriter progress)
{
    private const int LoggedLoadErrors = 5;

    private readonly object _outputLock = new();

    private readonly ConcurrentDictionary<ScenarioGroup, GameLoad> _loads = new();

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

        var groups = ScenarioGrouping.Group(scenarios, sessions.CreateRequiredModResolver());
        var outcomes = groups.Where(g => g.SkipReason != null).SelectMany(g => Skip(g, options.OutputDirectory)).ToList();
        var runnable = groups.Where(g => g.SkipReason == null).ToList();
        var queue = new WorkQueue(runnable);
        var workers = runnable.Count == 0
            ? 0
            : WorkerCount.Choose(options.Workers, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, Environment.ProcessorCount, runnable.Sum(g => g.Scenarios.Count));
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
        var loads = groups.Where(_loads.ContainsKey).Select(g => _loads[g]).ToList();
        RunReport.WriteJson(Path.Combine(options.OutputDirectory, "run.json"), ordered, loads);
        HtmlReport.Write(Path.Combine(options.OutputDirectory, "report.html"), ordered, loads);
        Emit(new JsonObject
        {
            ["scenarios"] = ordered.Count,
            ["failed"] = ordered.Count(o => o.Failed),
            ["skipped"] = ordered.Count(o => o.Skipped),
            ["workers"] = workers,
            ["loadErrors"] = loads.Sum(l => l.Errors.Count),
            ["loadErrorsFromModsUnderTest"] = loads.Sum(l => l.FromModsUnderTest.Count),
            ["seconds"] = Math.Round(timer.Elapsed.TotalSeconds, 1),
            ["playerFilesVerified"] = restore?.Verified,
            ["report"] = Path.Combine(options.OutputDirectory, "report.html"),
            ["json"] = Path.Combine(options.OutputDirectory, "run.json"),
        });
        return ordered;
    }

    /// <summary>Records a skipped group's scenarios as skipped, without booting a game.</summary>
    private IEnumerable<ScenarioOutcome> Skip(ScenarioGroup group, string outputRoot)
    {
        foreach (var scenario in group.Scenarios)
        {
            var outcome = ScenarioOutcome.Skip(scenario, group.Name, group.SkipReason!, OutputDirectoryOf(outputRoot, scenario));
            Progress($"Skipped {scenario.TypeName}: {group.SkipReason}.");
            EmitOutcome(outcome, worker: null);
            yield return outcome;
        }
    }

    /// <summary>Reads the errors a group's freshly booted game logged, once per group, and reports them.</summary>
    private void RecordLoad(ScenarioGroup group, SessionState session)
    {
        if (_loads.ContainsKey(group))
        {
            return;
        }

        var modsUnderTest = group.Mods.Select(m => ModFingerprint.Read(m.Directory, Sessions.InstalledDirectory(m.Directory))).ToList();
        var load = new GameLoad(group.Name, ReadLoggedErrors(session).Select(text => LoadErrorAttribution.Attribute(text, modsUnderTest)).ToList());
        if (!_loads.TryAdd(group, load) || load.Errors.Count == 0)
        {
            return;
        }

        var lines = new List<string>
        {
            $"Game load for {group.Name}: {load.FromModsUnderTest.Count} error(s) from mods under test, " +
            $"{load.Errors.Count - load.FromModsUnderTest.Count} from the base game or other mods.",
        };
        foreach (var fromModUnderTest in new[] { true, false })
        {
            var errors = load.Errors.Where(e => e.FromModUnderTest == fromModUnderTest).ToList();
            lines.AddRange(errors.Take(LoggedLoadErrors).Select(e =>
                $"  {(fromModUnderTest ? "Load error" : "Load warning")} ({e.Mod ?? "base game or another mod"}): {GameLoad.FirstLine(e.Text)}"));
            if (errors.Count > LoggedLoadErrors)
            {
                lines.Add($"  ...and {errors.Count - LoggedLoadErrors} more; see report.html.");
            }
        }

        Progress(string.Join(Environment.NewLine, lines));
    }

    private IReadOnlyList<string> ReadLoggedErrors(SessionState session)
    {
        try
        {
            var response = new HarnessClient(session.Port).Send("log", new JsonObject { ["minLevel"] = "error", ["count"] = 100000 });
            if (response["ok"]?.GetValue<bool>() == true)
            {
                return response["result"]?["entries"]?.AsArray().Select(e => e?["text"]?.ToString()).OfType<string>().ToList() ?? [];
            }

            Progress($"Could not read the game's load errors: {response["error"]}");
        }
        catch (Exception e) when (e is IOException or SocketException)
        {
            Progress($"Could not read the game's load errors: {e.Message}");
        }

        return [];
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

    private void EmitOutcome(ScenarioOutcome outcome, string? worker) => Emit(new JsonObject
    {
        ["scenario"] = outcome.Scenario.TypeName,
        ["worker"] = worker,
        ["passed"] = outcome.Passed,
        ["skipped"] = outcome.Skipped,
        ["attempts"] = outcome.Attempts,
        ["setupSeconds"] = Math.Round(outcome.SetupSeconds, 2),
        ["runSeconds"] = Math.Round(outcome.RunSeconds, 2),
        ["error"] = outcome.Error,
        ["skipReason"] = outcome.SkipReason,
    });

    private static string OutputDirectoryOf(string outputRoot, ScenarioClass scenario) =>
        Path.Combine(outputRoot, scenario.TypeName[(scenario.TypeName.LastIndexOf('.') + 1)..]);

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
                    batch.EmitOutcome(outcome, id);
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
            var outputDirectory = OutputDirectoryOf(options.OutputDirectory, scenario);
            ScenarioOutcome? outcome = null;
            for (var attempt = 1; attempt <= options.Retries + 1; attempt++)
            {
                double setup = 0;
                JsonObject report;
                try
                {
                    var session = EnsureGame(group);
                    if (batch._loads.GetValueOrDefault(group)?.Failure(options.AllowLoadErrors) is { } loadFailure)
                    {
                        return new ScenarioOutcome(scenario, group.Name, false, attempt, 0, 0, loadFailure,
                            new JsonObject { ["passed"] = false, ["error"] = loadFailure }, outputDirectory);
                    }

                    if (!_freshGame)
                    {
                        setup = ScenarioInvoker.NewGame(new HarnessClient(session.Port), group.Biome);
                    }

                    _freshGame = false;
                    var timer = Stopwatch.StartNew();
                    report = ScenarioInvoker.Run(new HarnessClient(session.Port), assemblies[scenario.ProjectFile], scenario.TypeName, outputDirectory, options.VideoFps);
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
                new LaunchOptions(group.Dlcs, group.Mods.Select(m => m with { Build = null }).ToList(), group.RequiredMods, group.Window, hidden ? 0 : options.Port, hidden, group.Biome),
                id, checkBusy: false);
            _freshGame = true;
            batch.RecordLoad(group, _session);
            return _session;
        }
    }
}
