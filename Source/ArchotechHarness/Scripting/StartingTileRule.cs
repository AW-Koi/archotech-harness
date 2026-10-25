using RimWorld.Planet;

namespace ArchotechHarness.Scripting;

/// <summary>How the harness ranks starting tiles: flat or small hills at 10 to 25C on average fit.</summary>
public static class StartingTileRule
{
    public const float MinTemperature = 10f;
    public const float MaxTemperature = 25f;

    public static bool Fits(Hilliness hilliness, float averageTemperature) =>
        hilliness is Hilliness.Flat or Hilliness.SmallHills && averageTemperature is >= MinTemperature and <= MaxTemperature;

    /// <summary>Higher is better: the biome and fitting, the biome, fitting ground to convert, then anything.</summary>
    public static int Preference(bool ofTheBiome, Hilliness hilliness, float averageTemperature) =>
        (ofTheBiome ? 2 : 0) + (Fits(hilliness, averageTemperature) ? 1 : 0);
}
