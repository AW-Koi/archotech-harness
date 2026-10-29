using System;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Game time. The speed set here applies as soon as the game can take it; anything may change it after.</summary>
public static class Clock
{
    public static int Tick => Find.TickManager.TicksGame;

    public static TimeSpeed Speed
    {
        get => Find.TickManager.CurTimeSpeed;
        set
        {
            var session = ScriptSession.Current ?? throw new InvalidOperationException("No harness script is running.");
            session.Speed.Ask(value);
            session.Timeline.Add("speed", value.ToString());
            Find.TickManager.CurTimeSpeed = value;
        }
    }

    /// <summary>Jumps the clock, without ticking, to the start of this hour today or tomorrow; stays put within the hour.</summary>
    public static void SkipToHour(int hour)
    {
        if (hour is < 0 or >= GenDate.HoursPerDay)
        {
            throw new HarnessException($"SkipToHour takes an hour from 0 to 23; it has {hour}.");
        }

        var ticks = TicksUntilHour(GenLocalDate.DayTick(World.Map), hour);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ticks);
        ScriptSession.Current?.Timeline.Add("clock", $"skipped {ticks} ticks to hour {hour}");
    }

    internal static int TicksUntilHour(int dayTick, int hour)
    {
        if (dayTick / GenDate.TicksPerHour == hour)
        {
            return 0;
        }

        return ((hour * GenDate.TicksPerHour - dayTick) % GenDate.TicksPerDay + GenDate.TicksPerDay) % GenDate.TicksPerDay;
    }
}
