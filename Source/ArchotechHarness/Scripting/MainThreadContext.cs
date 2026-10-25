using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ArchotechHarness.Scripting;

/// <summary>Runs script continuations on the game's main thread, one frame at a time.</summary>
public sealed class MainThreadContext : SynchronizationContext
{
    public static readonly MainThreadContext Instance = new();

    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object State)> _queue = new();

    private MainThreadContext()
    {
    }

    public override void Post(SendOrPostCallback d, object state) => _queue.Enqueue((d, state));

    public override void Send(SendOrPostCallback d, object state) =>
        throw new NotSupportedException("Scripts cannot block the main thread.");

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>Runs an action with this context installed, so awaits inside it resume on the main thread.</summary>
    public void Run(Action action)
    {
        var previous = Current;
        SetSynchronizationContext(this);
        try
        {
            action();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    /// <summary>Runs the continuations queued so far and returns how many ran.</summary>
    public int Drain()
    {
        var ran = 0;
        Run(() =>
        {
            var pending = _queue.Count;
            while (pending-- > 0 && _queue.TryDequeue(out var item))
            {
                item.Callback(item.State);
                ran++;
            }
        });
        return ran;
    }
}
