using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Entry point of the rwh command-line tool.</summary>
public static class Program
{
    private const string Usage = """
        rwh: drive RimWorld 1.7 through the dev harness mod.

          rwh launch [--dlc NAME]... [--mod DIR]... [--window WxH] [--port N]
          rwh run SCENARIO.csx|SCENARIO.json [--keep-open] [--out DIR] [--port N]
          rwh send COMMAND ['{"json":"args"}']
          rwh capture PATH.png
          rwh status
          rwh log [--count N] [--problems]
          rwh stop
          rwh restore
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
                "send" => Send(sessions, options),
                "capture" => Capture(sessions, options),
                "status" => Print(sessions.Describe()),
                "log" => Log(paths, options),
                "stop" => Print(Describe(sessions.Stop())),
                "restore" => Print(Describe(sessions.Finish())),
                "watchdog" => Watchdog(sessions),
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
        var isScript = Path.GetExtension(file).Equals(".csx", StringComparison.OrdinalIgnoreCase);
        var name = isScript ? Path.GetFileNameWithoutExtension(file) : ScenarioLoader.Load(file).Name;
        var runDirectory = options.Value("--out") is { } outDirectory
            ? Path.GetFullPath(outDirectory)
            : Path.Combine(paths.RunsDirectory, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}");
        var passed = isScript
            ? new ScriptScenarioRunner(paths, sessions, Console.Out).Run(file, runDirectory, options.Flag("--keep-open"), options.Port())
            : new ScenarioRunner(sessions, Console.Out).Run(ScenarioLoader.Load(file), runDirectory, options.Flag("--keep-open"), options.Port());
        return passed ? 0 : 1;
    }

    private static int Send(SessionManager sessions, CommandLineOptions options)
    {
        var session = sessions.Current ?? throw new ScenarioException("No session is running; use 'rwh launch' first.");
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
        var session = sessions.Current ?? throw new ScenarioException("No session is running.");
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

    private static int Watchdog(SessionManager sessions)
    {
        sessions.RunWatchdog();
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
