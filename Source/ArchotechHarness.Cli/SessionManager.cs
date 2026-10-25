using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Starts and ends harness sessions, one game each, and restores the player's files once the last one ends.</summary>
/// <remarks>The first session backs the files up and the last one restores them; a detached watchdog per session covers crashes.</remarks>
public sealed class SessionManager(HarnessPaths paths, TextWriter progress)
{
    public const string MainSessionId = "main";
    public const int FirstPort = 47801;

    private static readonly TimeSpan HarnessStartTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan MapLoadTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan QuitTimeout = TimeSpan.FromSeconds(60);

    public SessionState? Current => SessionState.Load(paths.SessionFile(MainSessionId)) ?? Sessions.FirstOrDefault();

    /// <summary>This checkout's sessions.</summary>
    public IReadOnlyList<SessionState> Sessions => MachineSessions.Where(Mine).ToList();

    /// <summary>Every archo session on the machine, whichever checkout started it.</summary>
    public IReadOnlyList<SessionState> MachineSessions =>
        Directory.Exists(paths.SessionsDirectory)
            ? Directory.EnumerateFiles(paths.SessionsDirectory, "*.json").Select(SessionState.Load).OfType<SessionState>().ToList()
            : [];

    public SessionState? Find(string id) => SessionState.Load(paths.SessionFile(id));

    /// <summary>Throws a clear message when another archo session, or a RimWorld outside archo, is running.</summary>
    public void ThrowIfBusy(IReadOnlyCollection<string>? ownSessions = null)
    {
        RecoverStaleSessions();
        var others = MachineSessions.Where(s => !Mine(s) || ownSessions?.Contains(s.Id) != true).ToList();
        if (others.Count > 0)
        {
            var other = others[0];
            var where = Mine(other) ? "this checkout" : other.Home;
            throw new SessionBusyException(
                $"Another archo session is running ({others.Count} game(s); '{other.Id}' from {where} has pid {other.GameProcessId}, started {other.StartedUtc.ToLocalTime():HH:mm:ss}). " +
                "Wait for it to finish (or pass --wait), attach to it with --attach, or run 'archo stop' there if it is yours.");
        }

        var ours = MachineSessions.Select(s => s.GameProcessId).ToHashSet();
        if (Process.GetProcessesByName("RimWorld").FirstOrDefault(p => !ours.Contains(p.Id)) is { } stranger)
        {
            throw new SessionBusyException(
                $"RimWorld is already running (pid {stranger.Id}) outside archo. Archo will not touch a game you may be playing; close it first.");
        }
    }

    /// <summary>Throws when a game from this checkout is running, naming the command that has to wait.</summary>
    public void ThrowIfOwnGameRunning(string command)
    {
        if (Sessions.Count > 0)
        {
            throw new SessionBusyException($"A game from this checkout is running; run 'archo stop' before '{command}'.");
        }
    }

    /// <summary>Waits until no other session or game is running, checking every few seconds.</summary>
    public void WaitUntilFree(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                ThrowIfBusy();
                return;
            }
            catch (SessionBusyException e) when (DateTime.UtcNow < deadline)
            {
                progress.WriteLine(e.Message + " Waiting.");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }

    public SessionState Start(LaunchOptions options) => Start(options, MainSessionId, checkBusy: true);

    /// <summary>Starts a game for a session id; a worker pool passes checkBusy false for its own later workers.</summary>
    public SessionState Start(LaunchOptions options, string id, bool checkBusy)
    {
        if (checkBusy)
        {
            ThrowIfBusy();
        }

        foreach (var mod in options.Mods.Where(m => m.Build != null))
        {
            Build(mod.Build!);
        }

        var packageIds = options.Mods.Select(m => ModFolder.ReadPackageId(m.Directory)).ToList();
        SessionState session;
        Process game;
        using (AcquireLock())
        {
            var guard = new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory);
            if (!guard.HasBackup)
            {
                guard.Backup();
            }

            EnsureInstalled(paths.HarnessModDirectory);
            foreach (var mod in options.Mods)
            {
                EnsureInstalled(mod.Directory);
            }

            var profile = paths.ProfileDirectory(id);
            var config = Path.Combine(profile, "Config");
            ProfileFiles.WriteModsConfig(Path.Combine(config, "ModsConfig.xml"), ModProfile.LoadOrder(options.Dlcs, packageIds), paths.GameVersion);
            ProfileFiles.WritePrefs(Path.Combine(config, "Prefs.xml"), options.Window);

            var port = options.Port > 0 ? options.Port : FreePort();
            var arguments = new List<string>
            {
                "-quicktest", $"\"-savedatafolder={profile}\"", $"-WindowSize={options.Window.Width},{options.Window.Height}",
                $"-harnessPort={port}", "-NoCrashDialog", "-DisableLogStackTraces",
            };
            if (options.Hidden)
            {
                arguments.AddRange(["-NoWindow", "-NoFileLog"]);
            }

            game = Process.Start(new ProcessStartInfo(paths.GameExecutable)
            {
                WorkingDirectory = paths.GameDirectory,
                UseShellExecute = true,
                Arguments = string.Join(' ', arguments),
            }) ?? throw new ScenarioException("RimWorld did not start.");
            session = new SessionState(id, paths.Home, game.Id, port, DateTime.UtcNow);
            session.Save(paths.SessionFile(id));
        }

        try
        {
            StartWatchdog(id);
            progress.WriteLine($"Started RimWorld for session '{id}' (pid {game.Id}); waiting for the harness on port {session.Port}.");
            WaitForMap(session, game);
            return session;
        }
        catch
        {
            if (!game.HasExited)
            {
                game.Kill();
                game.WaitForExit();
            }

            Finish(id);
            throw;
        }
    }

    /// <summary>Stops every session and returns the restore report of the last one to end.</summary>
    public RestoreReport? StopAll()
    {
        RestoreReport? report = null;
        foreach (var session in Sessions)
        {
            report = Stop(session.Id) ?? report;
        }

        return report ?? Finish();
    }

    public RestoreReport? Stop() => StopAll();

    /// <summary>Asks one game to quit, closes or kills it if it does not, then ends its session.</summary>
    /// <remarks>Returns the restore report when this was the last session, otherwise null.</remarks>
    public RestoreReport? Stop(string id)
    {
        var session = Find(id);
        if (session == null)
        {
            return null;
        }

        var game = TryGetProcess(session.GameProcessId);
        if (game != null)
        {
            try
            {
                new HarnessClient(session.Port).Send("quit");
            }
            catch (Exception e) when (e is IOException or SocketException)
            {
                game.CloseMainWindow();
            }

            if (!game.WaitForExit(QuitTimeout))
            {
                progress.WriteLine($"RimWorld for session '{id}' did not quit in time; killing it.");
                game.Kill();
                game.WaitForExit();
            }
        }

        return Finish(id);
    }

    /// <summary>Ends a session whose game has exited; the last session to end restores the player's files and removes the junctions.</summary>
    public RestoreReport? Finish(string id = MainSessionId)
    {
        using var stateLock = AcquireLock();
        var session = Find(id);
        if (session != null && TryGetProcess(session.GameProcessId) != null)
        {
            return null;
        }

        File.Delete(paths.SessionFile(id));
        DeleteStaleSessionFiles();
        if (MachineSessions.Count > 0)
        {
            return null;
        }

        JunctionRegistry.Load(paths.JunctionsFile).ForEach(Junctions.Remove);
        File.Delete(paths.JunctionsFile);
        var guard = new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory);
        if (!guard.HasBackup)
        {
            return null;
        }

        var report = guard.Restore();
        var log = Path.Combine(paths.BackupDirectory, PlayerConfigGuard.SessionLogFileName);
        if (File.Exists(log))
        {
            File.Copy(log, Path.Combine(paths.StateDirectory, "last-session-Player.log"), overwrite: true);
        }

        if (report.Verified)
        {
            Directory.Delete(paths.BackupDirectory, recursive: true);
        }

        return report;
    }

    public void RunWatchdog(string id)
    {
        if (Find(id) is { } session && TryGetProcess(session.GameProcessId) is { } game)
        {
            using var branding = new WindowBranding(paths.WindowIcon);
            branding.Maintain(game);
        }

        Finish(id);
    }

    public void Build(BuildCommand build)
    {
        progress.WriteLine($"Building: {build.Command} (in {build.WorkingDirectory})");
        var start = new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", build.Command },
            WorkingDirectory = build.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new ScenarioException($"Build failed ({process.ExitCode}):\n{output.Result}{error.Result}");
        }
    }

    public JsonObject Describe()
    {
        var sessions = Sessions;
        return new JsonObject
        {
            ["running"] = sessions.Any(s => TryGetProcess(s.GameProcessId) != null),
            ["sessions"] = new JsonArray(sessions.Select(s => (JsonNode)new JsonObject
            {
                ["id"] = s.Id,
                ["pid"] = s.GameProcessId,
                ["port"] = s.Port,
                ["startedUtc"] = s.StartedUtc,
                ["running"] = TryGetProcess(s.GameProcessId) != null,
            }).ToArray()),
        };
    }

    private bool Mine(SessionState session) => string.Equals(session.Home, paths.Home, StringComparison.OrdinalIgnoreCase);

    private void DeleteStaleSessionFiles()
    {
        foreach (var file in Directory.Exists(paths.SessionsDirectory) ? Directory.GetFiles(paths.SessionsDirectory, "*.json") : [])
        {
            if (SessionState.Load(file) is not { } session || TryGetProcess(session.GameProcessId) == null)
            {
                File.Delete(file);
            }
        }
    }

    private void RecoverStaleSessions()
    {
        var stale = MachineSessions.Where(s => TryGetProcess(s.GameProcessId) == null).ToList();
        var orphanedBackup = MachineSessions.Count == 0 && new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory).HasBackup;
        if (stale.Count == 0 && !orphanedBackup)
        {
            return;
        }

        progress.WriteLine("Found an unfinished session; cleaning it up and restoring the player's files if it was the last.");
        if (stale.Count > 0)
        {
            using (AcquireLock())
            {
                DeleteStaleSessionFiles();
            }
        }

        Finish();
    }

    private void WaitForMap(SessionState session, Process game)
    {
        var client = new HarnessClient(session.Port);
        var deadline = DateTime.UtcNow + HarnessStartTimeout;
        while (!client.TryPing())
        {
            ThrowIfGone(game, deadline, "the harness to start");
            Thread.Sleep(500);
        }

        progress.WriteLine($"Harness for session '{session.Id}' is up; waiting for the quick-test map.");
        deadline = DateTime.UtcNow + MapLoadTimeout;
        while (client.Send("status")["result"]?["playerHasControl"]?.GetValue<bool>() != true)
        {
            ThrowIfGone(game, deadline, "the map to load");
            Thread.Sleep(500);
        }
    }

    private static void ThrowIfGone(Process game, DateTime deadline, string what)
    {
        if (game.HasExited)
        {
            throw new ScenarioException($"RimWorld exited (code {game.ExitCode}) while waiting for {what}. See 'archo log'.");
        }

        if (DateTime.UtcNow > deadline)
        {
            throw new ScenarioException($"Timed out waiting for {what}.");
        }
    }

    /// <summary>Links a mod into the Mods folder unless some folder there already holds its package id.</summary>
    private void EnsureInstalled(string modDirectory)
    {
        var packageId = ModFolder.ReadPackageId(modDirectory);
        if (ModFolder.FindInstalled(paths.ModsDirectory, packageId) != null)
        {
            return;
        }

        var link = Path.Combine(paths.ModsDirectory, "archo-" + packageId.Replace('.', '-'));
        Junctions.Create(link, modDirectory);
        JunctionRegistry.Add(paths.JunctionsFile, link);
        progress.WriteLine($"Linked {modDirectory} into the Mods folder as {Path.GetFileName(link)}.");
    }

    private int FreePort()
    {
        var taken = MachineSessions.Select(s => s.Port).ToHashSet();
        for (var port = FirstPort; ; port++)
        {
            if (taken.Contains(port))
            {
                continue;
            }

            try
            {
                var probe = new TcpListener(IPAddress.Loopback, port);
                probe.Start();
                probe.Stop();
                return port;
            }
            catch (SocketException)
            {
                // The port is in use; try the next one.
            }
        }
    }

    private void StartWatchdog(string id)
    {
        var host = Environment.ProcessPath!;
        var start = new ProcessStartInfo(host) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden };
        if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(typeof(SessionManager).Assembly.Location);
        }

        start.ArgumentList.Add("watchdog");
        start.ArgumentList.Add("--session");
        start.ArgumentList.Add(id);
        Environment.SetEnvironmentVariable("ARCHO_HOME", paths.Home);
        Process.Start(start);
    }

    private FileStream AcquireLock()
    {
        Directory.CreateDirectory(paths.MachineStateDirectory);
        var path = paths.LockFile;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 600)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static Process? TryGetProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return process.HasExited || !process.ProcessName.Equals("RimWorld", StringComparison.OrdinalIgnoreCase) ? null : process;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
