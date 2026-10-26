using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace ArchotechHarness.Scripting;

/// <summary>Stops signal actions, such as ambushes and wake-ups, from acting while the running script keeps the world asleep.</summary>
[HarmonyPatch]
public static class CalmSignalPatch
{
    public static IEnumerable<MethodBase> TargetMethods() =>
        CalmTargets.DeclaredOverrides(typeof(SignalAction), nameof(SignalAction.Notify_SignalReceived));

    public static bool Prefix() => ScriptSession.Current is not { KeepAsleep: true };
}
