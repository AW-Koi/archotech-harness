using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The running script: its output folder, waits, held game speed and what it recorded.</summary>
public sealed class ScriptSession
{
    private readonly List<(Func<bool> Done, TaskCompletionSource<bool> Signal, DateTime Deadline, string What)> _waits = new();
    private readonly JsonObject _notes = new();
    private readonly JsonArray _checks = new();
    private readonly JsonArray _shots = new();
    private readonly DateTime _started = DateTime.UtcNow;

    private string _framesDirectory;
    private double _frameInterval;
    private DateTime _nextFrame;
    private int _frameIndex;

    private ScriptSession(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
        Builders = new PendingBuilders(description => Timeline.Add("build", description));
    }

    public static ScriptSession Current { get; private set; }

    public string OutputDirectory { get; }

    public TimeSpeed? HeldSpeed { get; set; }

    public bool AnyCheckFailed { get; private set; }

    /// <summary>While set, no incidents, wake-ups, signal actions or letters happen and the weather stays as it is.</summary>
    public bool Calm { get; set; }

    public PendingBuilders Builders { get; }

    /// <summary>Errors the game logged since the scenario started.</summary>
    public GameErrorTally GameErrors { get; } = new();

    /// <summary>The errors the scenario's [AllowGameErrors] attributes accept; any other fails it.</summary>
    public GameErrorAllowList AllowedErrors { get; set; } = GameErrorAllowList.None;

    public ScenarioTimeline Timeline { get; } = new();

    public static ScriptSession Begin(string outputDirectory) => Current = new ScriptSession(outputDirectory);

    public Task WaitFor(Func<bool> done, double timeoutSeconds, string what)
    {
        if (done())
        {
            return Task.CompletedTask;
        }

        var signal = new TaskCompletionSource<bool>();
        _waits.Add((done, signal, DateTime.UtcNow.AddSeconds(timeoutSeconds), what));
        return signal.Task;
    }

    /// <summary>Saves a screenshot this many times a second into the folder, for a video of the run.</summary>
    public void StartRecording(string framesDirectory, double framesPerSecond)
    {
        System.IO.Directory.CreateDirectory(framesDirectory);
        _framesDirectory = framesDirectory;
        _frameInterval = 1 / framesPerSecond;
        _nextFrame = DateTime.UtcNow;
    }

    /// <summary>Re-applies the held speed, records video frames and completes finished waits; runs once per frame on the main thread.</summary>
    public void Tick()
    {
        if (_framesDirectory != null && DateTime.UtcNow >= _nextFrame)
        {
            _nextFrame = DateTime.UtcNow.AddSeconds(_frameInterval);
            UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_framesDirectory, $"frame_{_frameIndex++:D5}.png"));
        }

        if (HeldSpeed is { } speed
            && CanHoldSpeed(Verse.Current.ProgramState, Verse.Current.Game?.World != null, Find.CurrentMap != null, LongEventHandler.AnyEventNowOrWaiting)
            && Find.TickManager.CurTimeSpeed != speed)
        {
            Find.TickManager.CurTimeSpeed = speed;
        }

        for (var i = _waits.Count - 1; i >= 0; i--)
        {
            var (done, signal, deadline, what) = _waits[i];
            try
            {
                if (done())
                {
                    _waits.RemoveAt(i);
                    signal.TrySetResult(true);
                }
                else if (DateTime.UtcNow > deadline)
                {
                    _waits.RemoveAt(i);
                    signal.TrySetException(new TimeoutException($"Timed out waiting for {what}."));
                }
            }
            catch (Exception e)
            {
                _waits.RemoveAt(i);
                signal.TrySetException(e);
            }
        }
    }

    /// <summary>Whether the game is playable, so setting the speed touches no half-loaded world, such as during a reload.</summary>
    public static bool CanHoldSpeed(ProgramState state, bool worldLoaded, bool mapLoaded, bool loading) =>
        state == ProgramState.Playing && worldLoaded && mapLoaded && !loading;

    public void Note(string key, object value, int line = 0)
    {
        _notes[key] = value == null ? null : JsonSerializer.SerializeToNode(value, value.GetType());
        Timeline.Add("note", $"{key} = {_notes[key]?.ToJsonString() ?? "null"}", line);
    }

    public void Check(bool passed, string what, int line = 0)
    {
        AnyCheckFailed |= !passed;
        _checks.Add(new JsonObject { ["check"] = what, ["passed"] = passed });
        Timeline.Add("check", what, line, passed: passed);
    }

    public void Shot(string path, int line = 0)
    {
        _shots.Add(path);
        Timeline.Add("shot", System.IO.Path.GetFileName(path), line);
    }

    public JsonObject Report(Task script)
    {
        var errors = GameErrors.Distinct
            .Select(e => (JsonNode)new JsonObject { ["text"] = e.Text, ["count"] = e.Count, ["allowed"] = AllowedErrors.Allows(e.Text) })
            .ToArray();
        var failure = script.Exception?.InnerException;
        if (failure == null)
        {
            foreach (var builder in Builders.Unfinished)
            {
                Check(false, builder);
            }
        }

        var errorFailure = GameErrors.FailureMessage(AllowedErrors);
        return new JsonObject
        {
            ["passed"] = failure == null && !AnyCheckFailed && errorFailure == null,
            ["seconds"] = Math.Round((DateTime.UtcNow - _started).TotalSeconds, 2),
            ["tick"] = Find.TickManager?.TicksGame,
            ["error"] = failure == null ? errorFailure : $"{failure.GetType().Name}: {failure.Message}",
            ["at"] = failure == null ? null : ScriptFrames(failure),
            ["checks"] = _checks.DeepClone(),
            ["notes"] = _notes.DeepClone(),
            ["shots"] = _shots.DeepClone(),
            ["gameErrors"] = new JsonArray(errors),
            ["timeline"] = Timeline.ToJson(),
            ["frames"] = _framesDirectory,
        };
    }

    public void End()
    {
        foreach (var wait in _waits)
        {
            wait.Signal.TrySetCanceled();
        }

        _waits.Clear();
        if (Current == this)
        {
            Current = null;
        }
    }

    private static JsonArray ScriptFrames(Exception failure) =>
        new((failure.StackTrace ?? "")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Contains(".cs:line"))
            .Where(l => !l.StartsWith("at ArchotechHarness.", StringComparison.Ordinal))
            .Select(l => (JsonNode)l)
            .ToArray());
}
