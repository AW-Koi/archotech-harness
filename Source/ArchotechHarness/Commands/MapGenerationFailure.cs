using System;
using HarmonyLib;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Records the exception that last stopped a map from generating, which sends the game back to the main menu.</summary>
[HarmonyPatch(typeof(GameAndMapInitExceptionHandlers), nameof(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap))]
public static class MapGenerationFailure
{
    public static Exception Last { get; set; }

    public static void Prefix(Exception e) => Last = e;
}
