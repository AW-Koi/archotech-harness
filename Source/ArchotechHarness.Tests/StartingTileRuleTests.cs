using ArchotechHarness.Scripting;
using RimWorld.Planet;

namespace ArchotechHarness.Tests;

public sealed class StartingTileRuleTests
{
    [Theory]
    [InlineData(Hilliness.Flat, 10f, true)]
    [InlineData(Hilliness.SmallHills, 25f, true)]
    [InlineData(Hilliness.Flat, 9.9f, false)]
    [InlineData(Hilliness.Flat, 25.1f, false)]
    [InlineData(Hilliness.LargeHills, 18f, false)]
    [InlineData(Hilliness.Mountainous, 18f, false)]
    public void Fits_ByHillinessAndAverageTemperature_AcceptsOnlyFlatishMildTiles(Hilliness hilliness, float temperature, bool fits)
    {
        Assert.Equal(fits, StartingTileRule.Fits(hilliness, temperature));
    }

    [Theory]
    [InlineData(true, Hilliness.Flat, 18f, 3)]
    [InlineData(true, Hilliness.Mountainous, -8f, 2)]
    [InlineData(false, Hilliness.SmallHills, 18f, 1)]
    [InlineData(false, Hilliness.Flat, -8f, 0)]
    public void Preference_OfATile_RanksTheBiomeFirstThenMildFlatishGround(bool ofTheBiome, Hilliness hilliness, float temperature, int expected)
    {
        Assert.Equal(expected, StartingTileRule.Preference(ofTheBiome, hilliness, temperature));
    }
}
