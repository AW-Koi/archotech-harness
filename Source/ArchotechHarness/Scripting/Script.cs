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

    /// <summary>Waits for game ticks to pass, so the game must not be paused.</summary>
    public static Task Ticks(int ticks, double timeoutSeconds = DefaultTimeoutSeconds, [CallerLineNumber] int line = 0)
    {
        var target = Find.TickManager.TicksGame + ticks;
        return Timed("ticks", $"{ticks} ticks", line, Session.WaitFor(() => Find.TickManager.TicksGame >= target, timeoutSeconds, $"{ticks} ticks"));
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
