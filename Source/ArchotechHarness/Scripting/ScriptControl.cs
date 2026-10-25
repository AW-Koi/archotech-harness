using System;

namespace ArchotechHarness.Scripting;

/// <summary>Pauses, resumes and steps a running script, and counts the wall time and game ticks spent paused.</summary>
/// <remarks>While paused the script stops at its next await, so waits neither finish nor time out.</remarks>
public sealed class ScriptControl(Func<DateTime> utcNow, Func<int> ticksGame)
{
    private TimeSpan _pausedBefore;
    private int _pausedTicksBefore;
    private DateTime _pausedAt;
    private int _pausedAtTick;

    public bool Paused { get; private set; }

    /// <summary>Set by Step: the script pauses again once it has run up to its next await.</summary>
    public bool Stepping { get; private set; }

    public TimeSpan PausedTime => _pausedBefore + (Paused ? utcNow() - _pausedAt : TimeSpan.Zero);

    /// <summary>Game ticks that passed while paused, such as when the player took over.</summary>
    public int PausedTicks => _pausedTicksBefore + (Paused ? ticksGame() - _pausedAtTick : 0);

    public void Pause()
    {
        Stepping = false;
        if (Paused)
        {
            return;
        }

        Paused = true;
        _pausedAt = utcNow();
        _pausedAtTick = ticksGame();
    }

    public void Resume()
    {
        Stepping = false;
        if (!Paused)
        {
            return;
        }

        _pausedBefore += utcNow() - _pausedAt;
        _pausedTicksBefore += ticksGame() - _pausedAtTick;
        Paused = false;
    }

    /// <summary>Resumes until the script has run up to its next await, then pauses again.</summary>
    public void Step()
    {
        Resume();
        Stepping = true;
    }

    /// <summary>Called after each frame's script continuations ran, with how many ran.</summary>
    public void ContinuationsRan(int count)
    {
        if (Stepping && count > 0)
        {
            Pause();
        }
    }
}
