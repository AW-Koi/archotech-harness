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
    private readonly JsonArray _attachments = new();
    private readonly DateTime _started = DateTime.UtcNow;

    private FastForwardRun _fastForward;
    private string _framesDirectory;
    private double _frameInterval;
    private DateTime _nextFrame;
    private int _frameIndex;

    private int _progress;

    private ScriptSession(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
        Control = new ScriptControl(() => DateTime.UtcNow, () => Find.TickManager?.TicksGame ?? 0);
        Builders = new PendingBuilders(description => Timeline.Add("build", description));
    }

    public static ScriptSession Current { get; private set; }

    public string OutputDirectory { get; }

    public TimeSpeed? HeldSpeed { get; set; }

    public bool AnyCheckFailed { get; private set; }

    /// <summary>While set, the storyteller, queued and quest incidents don't fire.</summary>
    public bool NoIncidents { get; set; }

    /// <summary>While set, dormant pawns, buildings and sleeping lords stay asleep, and signal actions don't act.</summary>
    public bool KeepAsleep { get; set; }

    /// <summary>While set, letters are dropped.</summary>
    public bool NoLetters { get; set; }

    /// <summary>While set, the weather stays as it is.</summary>
    public bool HoldWeather { get; set; }

    public PendingBuilders Builders { get; }

    /// <summary>While set, the harness ticks the game itself and the speed shows Ultrafast; a paused script ticks nothing.</summary>
    public bool FastForwarding => _fastForward != null && !Control.Paused;

    /// <summary>Errors the game logged since the scenario started.</summary>
    public GameErrorTally GameErrors { get; } = new();

    /// <summary>The errors the scenario's [AllowGameErrors] attributes accept; any other fails it.</summary>
    public GameErrorAllowList AllowedErrors { get; set; } = GameErrorAllowList.None;

    public ScenarioTimeline Timeline { get; } = new();

    /// <summary>Pauses, resumes and steps the script, such as from the debug panel.</summary>
    public ScriptControl Control { get; }

    /// <summary>The scenario's class name, for display.</summary>
    public string ScenarioName { get; set; }

    /// <summary>Whether the debug panel shows over the game while this script runs.</summary>
    public bool ShowPanel { get; set; }

    /// <summary>Game ticks the script has seen, leaving out ticks the player ran while it was paused.</summary>
    public int ScriptTicks => Find.TickManager.TicksGame - Control.PausedTicks;

    /// <summary>What the script waits for now, such as an Until condition; empty when it is not waiting.</summary>
    public IReadOnlyList<string> Waiting => _waits.Select(w => w.What).Concat(_fastForward == null ? [] : [_fastForward.What]).ToList();

    /// <summary>Stops the script at its next await and hands the game speed to the player.</summary>
    public void PauseScript()
    {
        Control.Pause();
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        Timeline.Add("debugger", "paused");
    }

    /// <summary>Lets the script carry on from where it paused, at the speed it last held.</summary>
    public void ResumeScript()
    {
        Control.Resume();
        Timeline.Add("debugger", "resumed");
    }

    /// <summary>Lets the script run up to its next await, then pauses it again.</summary>
    public void StepScript()
    {
        Control.Step();
        Timeline.Add("debugger", "step");
    }

    /// <summary>Waits finished and fast-forwards ended since the last call, so a step can tell the script moved on.</summary>
    public int TakeProgress()
    {
        var progress = _progress;
        _progress = 0;
        return progress;
    }

    public static ScriptSession Begin(string outputDirectory) => Current = new ScriptSession(outputDirectory);

    public Task WaitFor(Func<bool> done, double timeoutSeconds, string what)
    {
        if (done())
        {
            return Task.CompletedTask;
        }

        var signal = new TaskCompletionSource<bool>();
        _waits.Add((done, signal, ScriptNow.AddSeconds(timeoutSeconds), what));
        return signal.Task;
    }

    /// <summary>Runs game ticks back to back, a slice per frame, until done holds or maxTicks pass; fails with TimeoutException when failAtMax.</summary>
    /// <remarks>The speed shows Ultrafast meanwhile, and the previous held speed returns afterwards.</remarks>
    public Task FastForward(Func<bool> done, int maxTicks, bool failAtMax, string what)
    {
        if (_fastForward != null)
        {
            throw new InvalidOperationException("A fast-forward is already running; await it first.");
        }

        _fastForward = new FastForwardRun(done, Find.TickManager.TicksGame + maxTicks, failAtMax, what, HeldSpeed);
        HeldSpeed = TimeSpeed.Ultrafast;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
        return _fastForward.Signal.Task;
    }

    /// <summary>Saves a screenshot this many times a second into the folder, for a video of the run.</summary>
    public void StartRecording(string framesDirectory, double framesPerSecond)
    {
        System.IO.Directory.CreateDirectory(framesDirectory);
        _framesDirectory = framesDirectory;
        _frameInterval = 1 / framesPerSecond;
        _nextFrame = DateTime.UtcNow;
    }

    /// <summary>Records video frames and, unless paused, re-applies the held speed and completes finished waits; runs once per frame on the main thread.</summary>
    public void Tick()
    {
        if (_framesDirectory != null && DateTime.UtcNow >= _nextFrame)
        {
            _nextFrame = DateTime.UtcNow.AddSeconds(_frameInterval);
            UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_framesDirectory, $"frame_{_frameIndex++:D5}.png"));
        }

        if (Control.Paused)
        {
            return;
        }

        if (HeldSpeed is { } speed
            && CanHoldSpeed(Verse.Current.ProgramState, Verse.Current.Game?.World != null, Find.CurrentMap != null, LongEventHandler.AnyEventNowOrWaiting)
            && Find.TickManager.CurTimeSpeed != speed)
        {
            Find.TickManager.CurTimeSpeed = speed;
        }

        if (_fastForward != null)
        {
            StepFastForward();
        }

        for (var i = _waits.Count - 1; i >= 0; i--)
        {
            var (done, signal, deadline, what) = _waits[i];
            try
            {
                if (done())
                {
                    _waits.RemoveAt(i);
                    _progress++;
                    signal.TrySetResult(true);
                }
                else if (ScriptNow > deadline)
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

    private void StepFastForward()
    {
        var run = _fastForward;
        var sliceEnds = DateTime.UtcNow + FastForwardRun.Slice;
        try
        {
            while (!run.Done() && Find.TickManager.TicksGame < run.EndTick && DateTime.UtcNow < sliceEnds)
            {
                Find.TickManager.DoSingleTick();
            }

            if (run.Done())
            {
                EndFastForward(null);
            }
            else if (Find.TickManager.TicksGame >= run.EndTick)
            {
                EndFastForward(run.FailAtMax ? new TimeoutException($"Fast-forwarded to tick {run.EndTick} without {run.What}.") : null);
            }
        }
        catch (Exception e)
        {
            EndFastForward(e);
        }
    }

    private void EndFastForward(Exception failure)
    {
        var run = _fastForward;
        _fastForward = null;
        HeldSpeed = run.ResumeSpeed;
        _progress++;
        if (failure == null)
        {
            run.Signal.TrySetResult(true);
        }
        else
        {
            run.Signal.TrySetException(failure);
        }
    }

    /// <summary>Wall time less the time spent paused, which wait deadlines count in.</summary>
    private DateTime ScriptNow => DateTime.UtcNow - Control.PausedTime;

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

    /// <summary>The seed the game's random numbers started from.</summary>
    public int? Seed { get; set; }

    public void Attach(string path, int line = 0)
    {
        _attachments.Add(path);
        Timeline.Add("attach", System.IO.Path.GetFileName(path), line);
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
            ["attachments"] = _attachments.DeepClone(),
            ["seed"] = Seed,
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
        _fastForward?.Signal.TrySetCanceled();
        _fastForward = null;
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
