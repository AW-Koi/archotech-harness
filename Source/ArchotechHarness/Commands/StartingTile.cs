using System.Linq;
using ArchotechHarness.Scripting;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Picks the quick-test colony's tile for a requested biome, ranked by StartingTileRule.Preference.</summary>
public static class StartingTile
{
    /// <summary>The BiomeDef name new games start on; null leaves the choice to the game.</summary>
    public static string Biome { get; set; }

    /// <summary>When the world has no settleable tile of the biome, converts the best other tile to it.</summary>
    public static PlanetTile Choose(BiomeDef biome)
    {
        var layer = Find.WorldGrid.Surface;
        var ranked = Enumerable.Range(0, layer.TilesCount)
            .Select(i => layer[i])
            .Where(t => TileFinder.IsValidTileForNewSettlement(t.tile))
            .GroupBy(t => StartingTileRule.Preference(t.PrimaryBiome == biome, t.hilliness, t.temperature))
            .OrderByDescending(g => g.Key)
            .FirstOrDefault();
        if (ranked == null || !ranked.TryRandomElement(out var chosen))
        {
            return TileFinder.RandomStartingTile();
        }

        if (chosen.PrimaryBiome != biome)
        {
            Log.Message($"[ArchotechHarness] The world has no settleable {biome.defName} tile; converting a {chosen.PrimaryBiome.defName} tile.");
            chosen.PrimaryBiome = biome;
        }

        return chosen.tile;
    }
}
