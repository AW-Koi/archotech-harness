using HarmonyLib;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Skips the game's own tick loop while a fast-forward ticks the game, so the speed shown is real and no tick runs twice.</summary>
[HarmonyPatch(typeof(TickManager), nameof(TickManager.TickManagerUpdate))]
public static class FastForwardTickPatch
{
    public static bool Prefix() => ScriptSession.Current is not { FastForwarding: true };
}
