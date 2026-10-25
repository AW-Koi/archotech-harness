using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Entry point of the archo command-line tool.</summary>
public static class Program
{
    private const string Usage = """
        archo: drive RimWorld 1.7 through the Archotech Harness mod.

          archo launch [--dlc NAME]... [--mod DIR]... [--window WxH] [--port N]
          archo run SCENARIO.cs [--keep-open] [--attach] [--wait] [--video] [--out DIR] [--port N]
          archo test [PATH]... [--filter GLOB]... [--retries N] [--workers N|auto] [--wait] [--video] [--out DIR]
          archo send COMMAND ['{"json":"args"}'] [--session ID]
          archo capture PATH.png [--session ID]
          archo status
          archo log [--count N] [--problems]
          archo stop
          archo restore
          archo clean [--keep N|all] [--max-mb N] [--dry-run]
        """;

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            var paths = HarnessPaths.Detect();
            var sessions = new SessionManager(paths, Console.Error);
            var options = new CommandLineOptions(args.Skip(1).ToArray());
            return args[0] switch
            {
                "launch" => Launch(sessions, options),
                "run" => Run(paths, sessions, options),
                "test" => Test(paths, sessions, options),
                "send" => Send(sessions, options),
                "capture" => Capture(sessions, options),
                "status" => Print(sessions.Describe()),
                "log" => Log(paths, options),
                "stop" => Print(Describe(sessions.Stop())),
                "restore" => Print(Describe(sessions.Finish())),
                "clean" => Clean(paths, sessions, options),
                "watchdog" => Watchdog(sessions, options),
                _ => throw new ScenarioException($"Unknown command '{args[0]}'.\n{Usage}"),
            };
        }
        catch (ScenarioException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Launch(SessionManager sessions, CommandLineOptions options)
    {
        var mods = options.All("--mod").Select(dir => new ScenarioMod(Path.GetFullPath(dir), null)).ToList();
        var session = sessions.Start(new LaunchOptions(options.All("--dlc"), mods, options.Window(), options.Port()));
        return Print(new JsonObject { ["pid"] = session.GameProcessId, ["port"] = session.Port, ["mapLoaded"] = true });
    }

    private static int Run(HarnessPaths paths, SessionManager sessions, CommandLineOptions options)
    {
        var file = options.Positional(0, "scenario file");
        var runDirectory = options.Value("--out") is { } outDirectory
            ? Path.GetFullPath(outDirectory)
            : Path.Combine(paths.RunsDirectory, $"{Path.GetFileNameWithoutExtension(file)}-{DateTime.Now:yyyyMMdd-HHmmss}");
        WaitIfAsked(sessions, options);
        PruneOldRuns(paths, sessions);
        var passed = new ScenarioClassRunner(sessions, Console.Out)
            .Run(file, runDirectory, options.Flag("--keep-open"), options.Flag("--attach"), options.Port(), options.VideoFps());
        return passed ? 0 : 1;
    }

    private static int Test(HarnessPaths paths, SessionManager sessions, CommandLineOptions options)
    {
        var scenarioPaths = options.Positionals.Count > 0 ? options.Positionals : [Directory.GetCurrentDirectory()];
        var outputDirectory = options.Value("--out") is { } outDirectory
            ? Path.GetFullPath(outDirectory)
            : Path.Combine(paths.TestRunsDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        WaitIfAsked(sessions, options);
        PruneOldRuns(paths, sessions);
        var outcomes = new ScenarioBatch(sessions, Console.Out, Console.Error).Run(new BatchOptions(
            scenarioPaths, options.All("--filter"), int.Parse(options.Value("--retries") ?? "0"), outputDirectory, options.Port(), options.Value("--workers"), options.VideoFps()));
        return outcomes.All(o => o.Passed) ? 0 : 1;
    }

    private static void WaitIfAsked(SessionManager sessions, CommandLineOptions options)
    {
        if (options.Flag("--wait") && !options.Flag("--attach"))
        {
            sessions.WaitUntilFree(TimeSpan.FromHours(1));
        }
    }

    private static void PruneOldRuns(HarnessPaths paths, SessionManager sessions)
    {
        if (sessions.Sessions.Count > 0)
        {
            return;
        }

        var result = RunRetention.Prune([paths.RunsDirectory, paths.TestRunsDirectory], RetentionPolicy.FromEnvironment(), dryRun: false);
        if (result.Removed.Count > 0 || result.Failed.Count > 0)
        {
            Console.Error.WriteLine(
                $"Pruned {result.Removed.Count} old run folder(s), freeing {result.FreedBytes / (1024 * 1024)} MB; {result.Failed.Count} could not be deleted. " +
                "Set ARCHO_KEEP_RUNS and ARCHO_KEEP_MB to change what archo keeps.");
        }
    }

    private static int Clean(HarnessPaths paths, SessionManager sessions, CommandLineOptions options)
    {
        sessions.ThrowIfOwnGameRunning("archo clean");
        var policy = RetentionPolicy.Parse(
            options.Value("--keep") ?? Environment.GetEnvironmentVariable("ARCHO_KEEP_RUNS"),
            options.Value("--max-mb") ?? Environment.GetEnvironmentVariable("ARCHO_KEEP_MB"));
        var dryRun = options.Flag("--dry-run");
        var result = RunRetention.Prune([paths.RunsDirectory, paths.TestRunsDirectory], policy, dryRun);
        return Print(new JsonObject
        {
            ["dryRun"] = dryRun,
            ["removed"] = new JsonArray(result.Removed.Select(r => (JsonNode)r.Path).ToArray()),
            ["freedMegabytes"] = Math.Round(result.FreedBytes / (1024.0 * 1024.0), 1),
            ["failed"] = new JsonArray(result.Failed.Select(f => (JsonNode)f).ToArray()),
        });
    }

    private static int Send(SessionManager sessions, CommandLineOptions options)
    {
        var session = Session(sessions, options);
        var command = options.Positional(0, "command");
        var arguments = options.PositionalOrNull(1) is { } json
            ? JsonNode.Parse(json) as JsonObject ?? throw new ScenarioException("Arguments must be a JSON object.")
            : null;
        var response = new HarnessClient(session.Port).Send(command, arguments);
        Print(response);
        return response["ok"]?.GetValue<bool>() == true ? 0 : 1;
    }

    private static int Capture(SessionManager sessions, CommandLineOptions options)
    {
        var session = Session(sessions, options);
        var path = Path.GetFullPath(options.Positional(0, "output path"));
        var (width, height) = WindowCapture.Save(session.GameProcessId, path);
        return Print(new JsonObject { ["path"] = path, ["width"] = width, ["height"] = height });
    }

    private static int Log(HarnessPaths paths, CommandLineOptions options)
    {
        var log = File.Exists(paths.PlayerLog) && new SessionManager(paths, TextWriter.Null).Current != null
            ? paths.PlayerLog
            : Path.Combine(paths.StateDirectory, "last-session-Player.log");
        foreach (var entry in PlayerLogTail.Read(log, int.Parse(options.Value("--count") ?? "40"), options.Flag("--problems")))
        {
            Console.WriteLine(entry);
        }

        return 0;
    }

    private static SessionState Session(SessionManager sessions, CommandLineOptions options) =>
        (options.Value("--session") is { } id ? sessions.Find(id) : sessions.Current)
        ?? throw new ScenarioException("No session is running; use 'archo launch' first.");

    private static int Watchdog(SessionManager sessions, CommandLineOptions options)
    {
        sessions.RunWatchdog(options.Value("--session") ?? SessionManager.MainSessionId);
        return 0;
    }

    private static JsonObject Describe(RestoreReport? report) => report == null
        ? new JsonObject { ["restored"] = new JsonArray(), ["note"] = "nothing to restore" }
        : new JsonObject
        {
            ["restored"] = new JsonArray(report.Restored.Select(r => (JsonNode)r).ToArray()),
            ["verified"] = report.Verified,
        };

    private static int Print(JsonNode node)
    {
        Console.WriteLine(node.ToJsonString());
        return 0;
    }
}
