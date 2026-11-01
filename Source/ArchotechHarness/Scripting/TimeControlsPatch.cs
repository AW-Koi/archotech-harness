// ReSharper disable InconsistentNaming
using HarmonyLib;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Tells the running script when the player pauses or resumes through the time controls (the pause key and the
/// speed buttons), so the player's pause holds the script; the game's own pauses, such as on an error, don't.</summary>
[HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))]
public static class TimeControlsPatch
{
    public static void Prefix(out TimeSpeed __state) => __state = Find.TickManager.CurTimeSpeed;

    public static void Postfix(TimeSpeed __state)
    {
        var change = ChangeOf(__state, Find.TickManager.CurTimeSpeed);
        if (change != PlayerSpeedChange.None)
        {
            ScriptSession.Current?.PlayerChangedSpeed(change);
        }
    }

    public static PlayerSpeedChange ChangeOf(TimeSpeed before, TimeSpeed after) => (before, after) switch
    {
        (not TimeSpeed.Paused, TimeSpeed.Paused) => PlayerSpeedChange.Paused,
        (TimeSpeed.Paused, not TimeSpeed.Paused) => PlayerSpeedChange.Resumed,
        _ => PlayerSpeedChange.None,
    };
}
