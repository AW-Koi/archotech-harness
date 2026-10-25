using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ArchotechHarness.Commands;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Script-level helpers, imported with "using static": waiting, checks, notes and screenshots.</summary>
public static class Script
{
    private const double DefaultTimeoutSeconds = 120;

    private static ScriptSession Session =>
        ScriptSession.Current ?? throw new InvalidOperationException("No harness script is running.");

    /// <summary>Waits until the condition holds, checking once per frame; throws TimeoutException after the timeout.</summary>
    public static Task Until(Func<bool> condition, double timeoutSeconds = DefaultTimeoutSeconds,
        [CallerArgumentExpression(nameof(condition))] string what = null, [CallerLineNumber] int line = 0) =>
        Timed("until", what, line, Session.WaitFor(condition, timeoutSeconds, what));

    /// <summary>Waits for game ticks to pass, so the game must not be paused; ticks run while the script is paused don't count.</summary>
    public static Task Ticks(int ticks, double timeoutSeconds = DefaultTimeoutSeconds, [CallerLineNumber] int line = 0)
    {
        var session = Session;
        var target = session.ScriptTicks + ticks;
        return Timed("ticks", $"{ticks} ticks", line, session.WaitFor(() => session.ScriptTicks >= target, timeoutSeconds, $"{ticks} ticks"));
    }

    /// <summary>Runs this many game ticks as fast as the machine allows at about 60 frames a second; the speed shows Ultrafast meanwhile.</summary>
    public static Task FastForward(int ticks, [CallerLineNumber] int line = 0)
    {
        if (ticks < 1)
        {
            throw new HarnessException($"FastForward needs 1 or more ticks; it has {ticks}.");
        }

        return Timed("fast-forward", $"{ticks} ticks", line, Session.FastForward(() => false, ticks, failAtMax: false, $"{ticks} ticks"));
    }

    /// <summary>Runs game ticks as fast as possible until the condition holds; throws TimeoutException after maxTicks.</summary>
    public static Task FastForwardUntil(Func<bool> condition, int maxTicks,
        [CallerArgumentExpression(nameof(condition))] string what = null, [CallerLineNumber] int line = 0)
    {
        if (maxTicks < 1)
        {
            throw new HarnessException($"FastForwardUntil needs a limit of 1 or more ticks; it has {maxTicks}.");
        }

        return Timed("fast-forward", what, line, Session.FastForward(condition, maxTicks, failAtMax: true, what));
    }

    /// <summary>Copies a file, such as a transcript a mod wrote, into the run folder and lists it in the report; returns the copy's path.</summary>
    /// <remarks>The HTML report shows .txt, .log, .md, .json and .csv files inline and links any other file.</remarks>
    public static string AttachFile(string path, [CallerLineNumber] int line = 0)
    {
        if (!File.Exists(path))
        {
            throw new HarnessException($"AttachFile: {path} does not exist.");
        }

        var copy = Path.Combine(Session.OutputDirectory, Path.GetFileName(path));
        if (!string.Equals(Path.GetFullPath(copy), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(path, copy, overwrite: true);
        }

        Session.Attach(copy, line);
        return copy;
    }

    /// <summary>Writes text to a file of that name, such as "transcript.txt", in the run folder and lists it in the report; returns its path.</summary>
    public static string AttachText(string fileName, string text, [CallerLineNumber] int line = 0)
    {
        if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName)
        {
            throw new HarnessException($"AttachText needs a plain file name such as \"transcript.txt\"; it has \"{fileName}\".");
        }

        var path = Path.Combine(Session.OutputDirectory, fileName);
        File.WriteAllText(path, text);
        Session.Attach(path, line);
        return path;
    }

    /// <summary>Measures game ticks and frames per second over this many real seconds, notes both and returns ticks per second.</summary>
    public static Task<double> TicksPerSecond(double seconds, [CallerLineNumber] int line = 0) =>
        seconds > 0
            ? MeasureTicksPerSecond(seconds, line)
            : throw new HarnessException($"TicksPerSecond needs more than 0 seconds; it has {seconds}.");

    private static async Task<double> MeasureTicksPerSecond(double seconds, int line)
    {
        var (ticks, frames, started) = (Find.TickManager.TicksGame, UnityEngine.Time.frameCount, DateTime.UtcNow);
        await Timed("measure", $"ticks per second over {seconds} s", line,
            Session.WaitFor(() => (DateTime.UtcNow - started).TotalSeconds >= seconds, seconds + DefaultTimeoutSeconds, "the measuring window"));
        var elapsed = (DateTime.UtcNow - started).TotalSeconds;
        var tps = Math.Round((Find.TickManager.TicksGame - ticks) / elapsed, 1);
        Session.Note("ticksPerSecond", tps, line);
        Session.Note("framesPerSecond", Math.Round((UnityEngine.Time.frameCount - frames) / elapsed, 1), line);
        return tps;
    }

    public static Task Frames(int frames = 1)
    {
        var target = UnityEngine.Time.frameCount + frames;
        return Session.WaitFor(() => UnityEngine.Time.frameCount >= target, DefaultTimeoutSeconds, $"{frames} frames");
    }

    /// <summary>Saves the game, loads it back and waits until the map is playable again.</summary>
    /// <remarks>Things and pawns held from before go stale; look them up again on the reloaded map.</remarks>
    public static Task Reload(double timeoutSeconds = DefaultTimeoutSeconds, [CallerLineNumber] int line = 0)
    {
        const string saveName = "archo-reload";
        var previous = Current.Game;
        GameDataSaveLoader.SaveGame(saveName);
        GameDataSaveLoader.LoadGame(saveName);
        return Timed("reload", "save and reload", line, Session.WaitFor(() =>
        {
            var ready = Current.Game != null && Current.Game != previous && Current.ProgramState == ProgramState.Playing
                && Find.CurrentMap != null && !LongEventHandler.AnyEventNowOrWaiting;
            if (ready) File.Delete(GenFilePaths.FilePathForSavedGame(saveName));
            return ready;
        }, timeoutSeconds, "the reloaded game"));
    }

    /// <summary>Saves a screenshot, UI included, as name.png in the run folder and returns its path.</summary>
    public static async Task<string> Shot(string name, [CallerLineNumber] int line = 0)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A screenshot needs a name.", nameof(name));
        }

        var path = Path.IsPathRooted(name) ? name : Path.Combine(Session.OutputDirectory, name.EndsWith(".png") ? name : name + ".png");
        await Command("screenshot", new JsonObject { ["path"] = path });
        Session.Shot(path, line);
        return path;
    }

    /// <summary>Records a check; a failed check fails the run but the script carries on.</summary>
    public static bool Check(bool condition, [CallerArgumentExpression(nameof(condition))] string what = null,
        [CallerLineNumber] int line = 0)
    {
        Session.Check(condition, what, line);
        return condition;
    }

    /// <summary>Records a check and stops the script when it fails.</summary>
    public static void Require([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string what = null,
        [CallerLineNumber] int line = 0)
    {
        Session.Check(condition, what, line);
        if (!condition)
        {
            throw new ScriptCheckFailedException(what);
        }
    }

    /// <summary>Adds a named value to the run report.</summary>
    public static void Note(string key, object value, [CallerLineNumber] int line = 0) => Session.Note(key, value, line);

    /// <summary>Runs any harness command, as archo send would, and returns its JSON result.</summary>
    public static async Task<JsonNode> Command(string name, JsonObject arguments = null)
    {
        var result = CommandRegistry.Run(name, arguments ?? new JsonObject());
        if (result.Poll == null)
        {
            return result.Value;
        }

        JsonNode value = null;
        await Session.WaitFor(() => (value = result.Poll()) != null, DefaultTimeoutSeconds, name);
        return value;
    }

    /// <summary>Records a wait on the timeline when it ends, with how long it took and whether it timed out.</summary>
    private static async Task Timed(string kind, string what, int line, Task wait)
    {
        var timeline = Session.Timeline;
        var startedAt = timeline.Elapsed;
        try
        {
            await wait;
            timeline.Add(kind, what, line, startedAt, passed: true);
        }
        catch (TimeoutException)
        {
            timeline.Add(kind, what + " (timed out)", line, startedAt, passed: false);
            throw;
        }
    }

    internal static JsonNode CommandNow(string name, JsonObject arguments)
    {
        var result = CommandRegistry.Run(name, arguments);
        return result.Poll == null
            ? result.Value
            : throw new InvalidOperationException($"'{name}' is asynchronous; use await Command(\"{name}\").");
    }
}
