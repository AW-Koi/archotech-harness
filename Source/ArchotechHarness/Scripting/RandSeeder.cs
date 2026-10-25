using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Restarts the game's random numbers from a seed without the error Rand.Seed logs outside a pushed state.</summary>
internal static class RandSeeder
{
    private static readonly MethodInfo SetState = AccessTools.PropertySetter(typeof(Rand), "StateCompressed")
        ?? throw new MissingMethodException(nameof(Rand), "StateCompressed");

    public static void Seed(int seed) => SetState.Invoke(null, [(ulong)(uint)seed]);
}
