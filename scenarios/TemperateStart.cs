namespace Scenarios;

/// <summary>[Biome] starts the colony on a flat-ish temperate forest tile that is mild on average and not freezing.</summary>
[Biome(nameof(BiomeDefOf.TemperateForest))]
public sealed class TemperateStart : HarnessScenario
{
    public override async Task Run()
    {
        var tile = Find.WorldGrid[World.Map.Tile];
        var outdoors = World.Map.mapTemperature.OutdoorTemp;
        Note("averageTemperature", tile.temperature);
        Note("outdoorTemperature", outdoors);
        Note("hilliness", tile.hilliness.ToString());

        Check(World.Map.Biome == BiomeDefOf.TemperateForest);
        Check(tile.temperature is >= 10f and <= 25f);
        Check(tile.hilliness is RimWorld.Planet.Hilliness.Flat or RimWorld.Planet.Hilliness.SmallHills);
        Check(outdoors > 0f);

        Clock.SkipToHour(10);
        Camera.Look(World.Center, zoom: 24f);
        await Shot("forest");
    }
}
