using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse.AI.Group;

namespace ArchotechHarness.Scripting;

/// <summary>Keeps sleeping lords asleep through clamor and wake-up signals while the running script keeps the world asleep.</summary>
[HarmonyPatch]
public static class CalmLordWakePatch
{
    public static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(Lord), nameof(Lord.Notify_Clamor)),
        AccessTools.Method(typeof(Lord), nameof(Lord.Notify_DormancyWakeup)),
    ];

    public static bool Prefix() => ScriptSession.Current is not { KeepAsleep: true };
}
