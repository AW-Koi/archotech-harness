using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Starts and ends harness sessions, and always restores the player's files afterwards.</summary>
/// <remarks>A detached watchdog restores the files if this process dies while the game runs.</remarks>
public sealed class SessionManager(HarnessPaths paths, TextWriter progress)
{
    private static readonly TimeSpan HarnessStartTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan MapLoadTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan QuitTimeout = TimeSpan.FromSeconds(60);

    public SessionState? Current => SessionState.Load(paths.SessionFile);

    /// <summary>Throws a clear message when another archo session, or any RimWorld, is running.</summary>
    public void ThrowIfBusy()
    {
        RecoverStaleSession();
        if (Current is { } other)
        {
            throw new SessionBusyException(
                $"Another archo session is running (game pid {other.GameProcessId}, started {other.StartedUtc.ToLocalTime():HH:mm:ss}). " +
                "Wait for it to finish (or pass --wait), attach to it with --attach, or run 'archo stop' if it is yours.");
        }

        var running = Process.GetProcessesByName("RimWorld");
        if (running.Length > 0)
        {
            throw new SessionBusyException(
                $"RimWorld is already running (pid {running[0].Id}) outside archo. Archo will not touch a game you may be playing; close it first.");
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

    public SessionState Start(LaunchOptions options)
    {
        ThrowIfBusy();

        foreach (var mod in options.Mods.Where(m => m.Build != null))
        {
            Build(mod.Build!);
        }

        var packageIds = options.Mods.Select(m => ModFolder.ReadPackageId(m.Directory)).ToList();
        var guard = new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory);
        guard.Backup();
        var junctions = new List<string>();
        try
        {
            EnsureInstalled(paths.HarnessModDirectory, junctions);
            foreach (var mod in options.Mods)
            {
                EnsureInstalled(mod.Directory, junctions);
            }

            var config = Path.Combine(paths.ProfileDirectory, "Config");
            ProfileFiles.WriteModsConfig(Path.Combine(config, "ModsConfig.xml"), ModProfile.LoadOrder(options.Dlcs, packageIds), paths.GameVersion);
            ProfileFiles.WritePrefs(Path.Combine(config, "Prefs.xml"), options.Window);

            var game = Process.Start(new ProcessStartInfo(paths.GameExecutable)
            {
                WorkingDirectory = paths.GameDirectory,
                UseShellExecute = true,
                Arguments = string.Join(' ',
                    "-quicktest",
                    $"\"-savedatafolder={paths.ProfileDirectory}\"",
                    $"-WindowSize={options.Window.Width},{options.Window.Height}",
                    $"-harnessPort={options.Port}",
                    "-NoCrashDialog",
                    "-DisableLogStackTraces"),
            }) ?? throw new ScenarioException("RimWorld did not start.");

            var session = new SessionState(game.Id, options.Port, DateTime.UtcNow, junctions);
            session.Save(paths.SessionFile);
            StartWatchdog();
            progress.WriteLine($"Started RimWorld (pid {game.Id}); waiting for the harness on port {options.Port}.");
            WaitForMap(session, game);
            return session;
        }
        catch
        {
            if (Current == null)
            {
                junctions.ForEach(Junctions.Remove);
            }

            Finish();
            throw;
        }
    }

    /// <summary>Asks the game to quit, closes or kills it if it does not, then restores the player's files.</summary>
    public RestoreReport? Stop()
    {
        var session = Current;
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
            catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException)
            {
                game.CloseMainWindow();
            }

            if (!game.WaitForExit(QuitTimeout))
            {
                progress.WriteLine("RimWorld did not quit in time; killing it.");
                game.Kill();
                game.WaitForExit();
            }
        }

        return Finish();
    }

    /// <summary>Restores the player's files and removes the junctions of a session whose game has exited.</summary>
    public RestoreReport? Finish()
    {
        Directory.CreateDirectory(paths.SessionDirectory);
        using var finishLock = AcquireLock();
        var session = Current;
        if (session != null && TryGetProcess(session.GameProcessId) != null)
        {
            return null;
        }

        session?.CreatedJunctions.ToList().ForEach(Junctions.Remove);
        var guard = new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory);
        RestoreReport? report = null;
        if (guard.HasBackup)
        {
            report = guard.Restore();
            var log = Path.Combine(paths.BackupDirectory, PlayerConfigGuard.SessionLogFileName);
            if (File.Exists(log))
            {
                Directory.CreateDirectory(paths.StateDirectory);
                File.Copy(log, Path.Combine(paths.StateDirectory, "last-session-Player.log"), overwrite: true);
            }

            if (report.Verified)
            {
                Directory.Delete(paths.BackupDirectory, recursive: true);
            }
        }

        File.Delete(paths.SessionFile);
        return report;
    }

    public void RunWatchdog()
    {
        var session = Current;
        if (session == null)
        {
            return;
        }

        if (TryGetProcess(session.GameProcessId) is { } game)
        {
            using var branding = new WindowBranding(paths.WindowIcon);
            branding.Maintain(game);
        }

        Finish();
    }

    private void RecoverStaleSession()
    {
        var session = Current;
        if ((session != null && TryGetProcess(session.GameProcessId) == null)
            || (session == null && new PlayerConfigGuard(paths.PlayerDataDirectory, paths.BackupDirectory).HasBackup))
        {
            progress.WriteLine("Found an unfinished session; restoring the player's files first.");
            Finish();
        }
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

        progress.WriteLine("Harness is up; waiting for the quick-test map.");
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
            throw new ScenarioException($"Timed out waiting for {what}. The game is still running; see 'archo log', then 'archo stop'.");
        }
    }

    private void EnsureInstalled(string modDirectory, List<string> junctions)
    {
        var packageId = ModFolder.ReadPackageId(modDirectory);
        if (ModFolder.FindInstalled(paths.ModsDirectory, packageId) != null)
        {
            return;
        }

        var link = Path.Combine(paths.ModsDirectory, "archo-" + packageId.Replace('.', '-'));
        Junctions.Create(link, modDirectory);
        junctions.Add(link);
        progress.WriteLine($"Linked {modDirectory} into the Mods folder as {Path.GetFileName(link)}.");
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

    private void StartWatchdog()
    {
        var host = Environment.ProcessPath!;
        var start = new ProcessStartInfo(host) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden };
        if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(typeof(SessionManager).Assembly.Location);
        }

        start.ArgumentList.Add("watchdog");
        Environment.SetEnvironmentVariable("ARCHO_HOME", paths.Home);
        Process.Start(start);
    }

    private FileStream AcquireLock()
    {
        var path = Path.Combine(paths.SessionDirectory, "finish.lock");
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 100)
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

    public JsonObject Describe()
    {
        var session = Current;
        return session == null
            ? new JsonObject { ["running"] = false }
            : new JsonObject
            {
                ["running"] = TryGetProcess(session.GameProcessId) != null,
                ["pid"] = session.GameProcessId,
                ["port"] = session.Port,
                ["startedUtc"] = session.StartedUtc,
            };
    }
}
