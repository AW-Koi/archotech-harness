using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DevHarness.Commands;
using DevHarness.Scripting;

namespace DevHarness.Server;

/// <summary>Hands commands from the socket threads to the game's main thread.</summary>
public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<PendingCommand> Incoming = new();
    private static readonly List<PendingCommand> Waiting = new();
    private static readonly List<(int Frames, Action Action)> Delayed = new();

    public static void Enqueue(PendingCommand command) => Incoming.Enqueue(command);

    /// <summary>Runs an action on the main thread after the given number of frames.</summary>
    public static void RunLater(int frames, Action action) => Delayed.Add((frames, action));

    public static void Pump()
    {
        while (Incoming.TryDequeue(out var command))
        {
            Start(command);
        }

        if (ScriptSession.Current is { } script)
        {
            MainThreadContext.Instance.Run(script.Tick);
        }

        MainThreadContext.Instance.Drain();

        for (var i = Waiting.Count - 1; i >= 0; i--)
        {
            var command = Waiting[i];
            if (Evaluate(command))
            {
                Waiting.RemoveAt(i);
            }
        }

        for (var i = Delayed.Count - 1; i >= 0; i--)
        {
            var (frames, action) = Delayed[i];
            if (frames > 0)
            {
                Delayed[i] = (frames - 1, action);
                continue;
            }

            Delayed.RemoveAt(i);
            action();
        }
    }

    private static void Start(PendingCommand command)
    {
        try
        {
            var result = CommandRegistry.Run(command.Name, command.Arguments);
            if (result.Poll == null)
            {
                command.Succeed(result.Value);
                return;
            }

            command.Poll = result.Poll;
            if (!Evaluate(command))
            {
                Waiting.Add(command);
            }
        }
        catch (Exception e)
        {
            Report(command, e);
        }
    }

    private static bool Evaluate(PendingCommand command)
    {
        try
        {
            var value = command.Poll();
            if (value != null)
            {
                command.Succeed(value);
                return true;
            }

            if (DateTime.UtcNow > command.Deadline)
            {
                command.Fail($"'{command.Name}' timed out.", "Timeout");
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Report(command, e);
            return true;
        }
    }

    private static void Report(PendingCommand command, Exception e)
    {
        if (e is HarnessException)
        {
            command.Fail(e.Message, nameof(HarnessException));
            return;
        }

        Verse.Log.Warning($"[DevHarness] '{command.Name}' threw: {e}");
        command.Fail(e.ToString(), e.GetType().Name);
    }
}
