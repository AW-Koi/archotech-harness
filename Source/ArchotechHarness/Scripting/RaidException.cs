namespace ArchotechHarness.Scripting;

/// <summary>A raid didn't fire, and <see cref="Step"/> says where it failed.</summary>
public sealed class RaidException(RaidStep step, string message) : HarnessException(message)
{
    public RaidStep Step { get; } = step;
}
