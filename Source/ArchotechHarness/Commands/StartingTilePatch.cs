using HarmonyLib;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Starts new games on StartingTile.Biome when it names a biome colonies can settle.</summary>
[HarmonyPatch(typeof(GameInitData), nameof(GameInitData.ChooseRandomStartingTile))]
public static class StartingTilePatch
{
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(GameInitData __instance)
    {
        if (StartingTile.Biome == null || DefDatabase<BiomeDef>.GetNamedSilentFail(StartingTile.Biome) is not { canBuildBase: true } biome)
        {
            return true;
        }

        __instance.startingTile = StartingTile.Choose(biome);
        return false;
    }
}
