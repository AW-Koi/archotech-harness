namespace ArchotechHarness.Cli.Tests;

public sealed class ScenarioDiscoveryTests : IDisposable
{
    private const string TwoScenarios = """
        namespace MopMod.Scenarios;

        public sealed class MopsTheFloor : HarnessScenario { public override Task Run() => Task.CompletedTask; }

        public sealed class SweepsTheHall : HarnessScenario { public override Task Run() => Task.CompletedTask; }
        """;

    private const string OneScenario = """
        namespace MopMod.Scenarios;

        public sealed class ScrubsTheTub : HarnessScenario { public override Task Run() => Task.CompletedTask; }
        """;

    private readonly TempDirectory _temp = new();

    public ScenarioDiscoveryTests()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        _temp.WriteFile("MopMod/harness/Cleaning.cs", TwoScenarios);
        _temp.WriteFile("MopMod/harness/Bathroom/Tub.cs", OneScenario);
        _temp.WriteFile("MopMod/harness/Helpers.cs", "public static class Helpers { }");
        _temp.WriteFile("MopMod/harness/obj/Generated.cs", OneScenario.Replace("ScrubsTheTub", "Generated"));
        _temp.WriteFile("MopMod/harness/bin/Copy.cs", OneScenario.Replace("ScrubsTheTub", "Copied"));
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Discover_Folder_FindsEveryScenarioClassOutsideBuildOutput()
    {
        var found = ScenarioDiscovery.Discover([_temp.Combine("MopMod", "harness")]);

        Assert.Equal(
            ["MopMod.Scenarios.ScrubsTheTub", "MopMod.Scenarios.MopsTheFloor", "MopMod.Scenarios.SweepsTheHall"],
            found.Select(s => s.TypeName));
    }

    [Fact]
    public void Discover_ProjectFile_SearchesTheProjectFolder()
    {
        var found = ScenarioDiscovery.Discover([_temp.Combine("MopMod", "harness", "Scenarios.csproj")]);

        Assert.Equal(3, found.Count);
    }

    [Fact]
    public void Discover_SourceFile_ReturnsItsScenariosWithTheirFile()
    {
        var file = _temp.Combine("MopMod", "harness", "Cleaning.cs");

        var found = ScenarioDiscovery.Discover([file]);

        Assert.Equal(["MopMod.Scenarios.MopsTheFloor", "MopMod.Scenarios.SweepsTheHall"], found.Select(s => s.TypeName));
        Assert.All(found, s => Assert.Equal(file, s.SourceFile));
    }

    [Fact]
    public void Discover_MissingPath_Throws()
    {
        Assert.Throws<ScenarioException>(() => ScenarioDiscovery.Discover([_temp.Combine("NoSuchFolder")]));
    }
}
