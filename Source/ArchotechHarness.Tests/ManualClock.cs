namespace ArchotechHarness.Tests;

/// <summary>Wall time and game ticks that move only when a test moves them.</summary>
public sealed class ManualClock
{
    public DateTime Now { get; private set; } = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    public int Tick { get; private set; } = 1000;

    public void Advance(TimeSpan time, int ticks = 0)
    {
        Now += time;
        Tick += ticks;
    }
}
