using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace ArchotechHarness.Scripting;

/// <summary>Keeps dormant pawns and buildings asleep while the running script keeps the world asleep.</summary>
[HarmonyPatch]
public static class CalmDormancyPatch
{
    public static IEnumerable<MethodBase> TargetMethods() =>
        CalmTargets.DeclaredOverrides(typeof(CompCanBeDormant), nameof(CompCanBeDormant.WakeUp));

    public static bool Prefix() => ScriptSession.Current is not { KeepAsleep: true };
}
