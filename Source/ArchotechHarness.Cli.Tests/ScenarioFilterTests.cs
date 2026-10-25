namespace ArchotechHarness.Cli.Tests;

public class ScenarioFilterTests
{
    private const string Spotless = "MopMod.Scenarios.SpotlessMoment";

    [Theory]
    [InlineData("SpotlessMoment", true)]
    [InlineData("spotless*", true)]
    [InlineData("*Moment", true)]
    [InlineData("MopMod.Scenarios.*", true)]
    [InlineData("Spotless?oment", true)]
    [InlineData("Pacifist*", false)]
    [InlineData("Spotless", false)]
    public void Matches_Pattern_MatchesTheSimpleOrFullName(string pattern, bool matches)
    {
        var filter = new ScenarioFilter([pattern]);

        Assert.Equal(matches, filter.Matches(Spotless));
    }

    [Fact]
    public void Matches_NoPatterns_MatchesEverything()
    {
        var filter = new ScenarioFilter([]);

        Assert.True(filter.Matches(Spotless));
    }

    [Fact]
    public void Matches_SeveralPatterns_MatchesAny()
    {
        var filter = new ScenarioFilter(["Pacifist*", "*Moment"]);

        Assert.True(filter.Matches(Spotless));
    }
}
