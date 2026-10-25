namespace DevHarness.Cli.Tests;

public sealed class ScenarioLoaderTests : IDisposable
{
    private const string MopScenario = """
        {
          "name": "mop-test",
          "dlcs": ["biotech"],
          "mods": [{ "dir": "../MopMod/mod", "build": { "command": "dotnet build", "workingDir": "../MopMod" } }],
          "steps": [
            { "cmd": "speed", "speed": "paused" },
            { "cmd": "probe", "name": "filthCount", "save": "before", "expect": { "path": "count", "op": "gt", "value": 0 } },
            { "cmd": "screenshot", "path": "after.png", "always": true }
          ]
        }
        """;

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Load_ModDirectory_ResolvesRelativeToTheScenarioFile()
    {
        var path = _temp.WriteFile("scenarios/mop-test.json", MopScenario);

        var scenario = ScenarioLoader.Load(path);

        var mod = Assert.Single(scenario.Mods);
        Assert.Equal(_temp.Combine("MopMod", "mod"), mod.Directory);
        Assert.Equal(_temp.Combine("MopMod"), mod.Build!.WorkingDirectory);
    }

    [Fact]
    public void Load_StepArguments_ExcludeTheRunnerKeys()
    {
        var path = _temp.WriteFile("scenarios/mop-test.json", MopScenario);

        var scenario = ScenarioLoader.Load(path);

        var probe = scenario.Steps[1];
        Assert.Equal("probe", probe.Command);
        Assert.Equal("before", probe.SaveAs);
        Assert.Equal("""{"name":"filthCount"}""", probe.Arguments.ToJsonString());
        Assert.Equal("count", probe.Expectation!.Path);
    }

    [Fact]
    public void Load_StepMarkedAlways_IsFlagged()
    {
        var path = _temp.WriteFile("scenarios/mop-test.json", MopScenario);

        var scenario = ScenarioLoader.Load(path);

        Assert.True(scenario.Steps[2].Always);
    }

    [Fact]
    public void Load_StepWithoutCommand_Throws()
    {
        var path = _temp.WriteFile("broken.json", """{ "name": "broken", "steps": [ { "speed": "paused" } ] }""");

        Assert.Throws<ScenarioException>(() => ScenarioLoader.Load(path));
    }
}
