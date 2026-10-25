namespace DevHarness.Cli.Tests;

public sealed class ScriptSourceTests : IDisposable
{
    private const string MopScript = """
        #mod "../MopMod/mod"
        #build "dotnet build MopMod.csproj" in "../MopMod"
        #dlc biotech
        #window 1280x720
        #load "helpers.cs"
        using System.Text;

        var room = 1;
        """;

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Parse_ModAndBuildDirectives_ResolveRelativeToTheScript()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", MopScript);

        var script = ScriptSource.Parse(path);

        var mod = Assert.Single(script.Mods);
        Assert.Equal(_temp.Combine("MopMod", "mod"), mod.Directory);
        Assert.Equal("dotnet build MopMod.csproj", mod.Build!.Command);
        Assert.Equal(_temp.Combine("MopMod"), mod.Build.WorkingDirectory);
    }

    [Fact]
    public void Parse_DlcAndWindowDirectives_SetTheProfile()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", MopScript);

        var script = ScriptSource.Parse(path);

        Assert.Equal(["biotech"], script.Dlcs);
        Assert.Equal(new WindowSize(1280, 720), script.Window);
    }

    [Fact]
    public void Parse_LoadDirective_ListsTheHelperFile()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", MopScript);

        var script = ScriptSource.Parse(path);

        Assert.Equal([_temp.Combine("scenarios", "helpers.cs")], script.HelperFiles);
    }

    [Fact]
    public void Parse_Body_BlanksDirectivesAndKeepsLineNumbers()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", MopScript);

        var script = ScriptSource.Parse(path);

        var lines = script.Body.Split('\n');
        Assert.Equal("", lines[0].Trim());
        Assert.Equal("var room = 1;", lines[7].Trim());
    }

    [Fact]
    public void Parse_UnknownDirective_Throws()
    {
        var path = _temp.WriteFile("broken.csx", "#teleport home\n");

        Assert.Throws<ScenarioException>(() => ScriptSource.Parse(path));
    }
}
