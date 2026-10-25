using System;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Game time. The speed set here holds until changed, even while the game refuses input.</summary>
public static class Clock
{
    public static int Tick => Find.TickManager.TicksGame;

    public static TimeSpeed Speed
    {
        get => Find.TickManager.CurTimeSpeed;
        set
        {
            var session = ScriptSession.Current ?? throw new InvalidOperationException("No harness script is running.");
            session.HeldSpeed = value;
            session.Timeline.Add("speed", value.ToString());
            Find.TickManager.CurTimeSpeed = value;
        }
    }
}
