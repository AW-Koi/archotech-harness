using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The game speed a script asked for: applied once, as soon as the game can take it, and never forced again, so
/// other code may change the speed in between.</summary>
public sealed class SpeedRequest
{
    private bool _due;

    /// <summary>The speed the script last asked for, to return to after a pause or fast-forward; null before it asks.</summary>
    public TimeSpeed? Last { get; private set; }

    public void Ask(TimeSpeed speed)
    {
        Last = speed;
        _due = true;
    }

    /// <summary>Asks for the last speed again, such as when the debugger resumes the script.</summary>
    public void AskAgain() => _due = Last != null;

    /// <summary>The speed to apply now, once; null when nothing is due or the game can't take it yet.</summary>
    public TimeSpeed? TakeDue(bool canApply)
    {
        if (!_due || !canApply)
        {
            return null;
        }

        _due = false;
        return Last;
    }
}
