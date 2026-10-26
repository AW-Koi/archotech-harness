using HarmonyLib;
using RimWorld;

namespace ArchotechHarness.Scripting;

/// <summary>Stops storyteller, queued and quest interval incidents from firing while the running script has no incidents.</summary>
[HarmonyPatch(typeof(Storyteller), nameof(Storyteller.TryFire))]
public static class CalmStorytellerPatch
{
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(ref bool __result)
    {
        if (ScriptSession.Current is not { NoIncidents: true })
        {
            return true;
        }

        __result = false;
        return false;
    }
}
