namespace ArchotechHarness.Cli.Tests;

public class ScenarioGroupingTests
{
    private static readonly ScenarioMod MopMod = new(Path.Combine("Mods", "MopMod", "mod"), null);
    private static readonly ScenarioMod BroomMod = new(Path.Combine("Mods", "BroomMod"), null);
    private static readonly WindowSize Window = new(1600, 900);

    [Fact]
    public void Group_SameModsInAnyOrder_ShareOneBoot()
    {
        var first = Scenario("A", [MopMod, BroomMod], []);
        var second = Scenario("B", [BroomMod, MopMod], []);

        var groups = ScenarioGrouping.Group([first, second]);

        var group = Assert.Single(groups);
        Assert.Equal(["A", "B"], group.Scenarios.Select(s => s.TypeName));
    }

    [Fact]
    public void Group_DifferentDlcs_SplitIntoSeparateBootsInFirstSeenOrder()
    {
        var plain = Scenario("Plain", [MopMod], []);
        var biotech = Scenario("Biotech", [MopMod], ["biotech"]);
        var plainAgain = Scenario("PlainAgain", [MopMod], []);

        var groups = ScenarioGrouping.Group([plain, biotech, plainAgain]);

        Assert.Equal(["MopMod", "MopMod+biotech"], groups.Select(g => g.Name));
        Assert.Equal(["Plain", "PlainAgain"], groups[0].Scenarios.Select(s => s.TypeName));
    }

    [Fact]
    public void Group_NoModsOrDlcs_IsNamedCore()
    {
        var groups = ScenarioGrouping.Group([Scenario("Smoke", [], [])]);

        Assert.Equal("core", Assert.Single(groups).Name);
    }

    private static ScenarioClass Scenario(string name, ScenarioMod[] mods, string[] dlcs) =>
        new(name, Path.Combine("Mods", "harness", "Scenarios.cs"), Path.Combine("Mods", "harness", "Scenarios.csproj"), mods, dlcs, Window);
}
