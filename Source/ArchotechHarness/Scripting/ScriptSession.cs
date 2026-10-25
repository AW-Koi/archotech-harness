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
    private readonly int _errorsAtStart;
    private readonly DateTime _started = DateTime.UtcNow;

    private ScriptSession(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
        _errorsAtStart = Log.Messages.Count(m => m.type == LogMessageType.Error);
    }

    public static ScriptSession Current { get; private set; }

    public string OutputDirectory { get; }

    public TimeSpeed? HeldSpeed { get; set; }

    public bool AnyCheckFailed { get; private set; }

    public PendingBuilders Builders { get; } = new();

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

    /// <summary>Re-applies the held speed and completes finished waits; runs once per frame on the main thread.</summary>
    public void Tick()
    {
        if (HeldSpeed is { } speed && Find.TickManager is { } ticks && ticks.CurTimeSpeed != speed && Verse.Current.ProgramState == ProgramState.Playing)
        {
            ticks.CurTimeSpeed = speed;
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

    public void Note(string key, object value) =>
        _notes[key] = value == null ? null : JsonSerializer.SerializeToNode(value, value.GetType());

    public void Check(bool passed, string what)
    {
        AnyCheckFailed |= !passed;
        _checks.Add(new JsonObject { ["check"] = what, ["passed"] = passed });
    }

    public void Shot(string path) => _shots.Add(path);

    public JsonObject Report(Task script)
    {
        var errors = Log.Messages.Where(m => m.type == LogMessageType.Error).Skip(_errorsAtStart).Select(m => (JsonNode)m.text).ToArray();
        var failure = script.Exception?.InnerException;
        if (failure == null)
        {
            foreach (var builder in Builders.Unfinished)
            {
                Check(false, builder);
            }
        }

        return new JsonObject
        {
            ["passed"] = failure == null && !AnyCheckFailed,
            ["seconds"] = Math.Round((DateTime.UtcNow - _started).TotalSeconds, 2),
            ["tick"] = Find.TickManager?.TicksGame,
            ["error"] = failure == null ? null : $"{failure.GetType().Name}: {failure.Message}",
            ["at"] = failure == null ? null : ScriptFrames(failure),
            ["checks"] = _checks.DeepClone(),
            ["notes"] = _notes.DeepClone(),
            ["shots"] = _shots.DeepClone(),
            ["gameErrors"] = new JsonArray(errors),
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
