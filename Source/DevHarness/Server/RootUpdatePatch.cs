using HarmonyLib;
using Verse;

namespace DevHarness.Server;

/// <summary>Runs queued commands on the main thread once per frame.</summary>
[HarmonyPatch(typeof(Root), nameof(Root.Update))]
public static class RootUpdatePatch
{
    public static void Postfix() => MainThreadDispatcher.Pump();
}
