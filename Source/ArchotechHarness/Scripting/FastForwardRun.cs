using System;
using System.Threading.Tasks;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>One fast-forward in progress: what ends it, the last tick it may reach and the speed to restore afterwards.</summary>
internal sealed class FastForwardRun(Func<bool> done, int endTick, bool failAtMax, string what, TimeSpeed? resumeSpeed)
{
    /// <summary>The real time each frame spends ticking, leaving enough of the frame to render about 60 frames a second.</summary>
    public static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(12);

    public Func<bool> Done { get; } = done;

    public int EndTick { get; } = endTick;

    public bool FailAtMax { get; } = failAtMax;

    public string What { get; } = what;

    public TimeSpeed? ResumeSpeed { get; } = resumeSpeed;

    public TaskCompletionSource<bool> Signal { get; } = new();
}
