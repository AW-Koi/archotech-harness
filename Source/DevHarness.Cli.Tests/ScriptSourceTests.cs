namespace DevHarness.Cli.Tests;

public sealed class ScriptSourceTests : IDisposable
{
    private const string MopScript = """
        // @mod "../MopMod/mod"
        // @build "dotnet build MopMod.csproj" in "../MopMod"
        // @dlc biotech
        // @window 1280x720
        #load "helpers.cs"
        #r "../MopMod/mod/1.7/Assemblies/MopMod.dll"
        using System.Text;

        var room = 1;
        """;

    private const string EditorScript = """
        #r "C:/Games/RimWorld/Assembly-CSharp.dll"
        using Verse;
        using static DevHarness.Scripting.Script;
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
    public void Parse_Body_BlanksDirectivesAndReferencesAndKeepsLineNumbers()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", MopScript);

        var script = ScriptSource.Parse(path);

        var lines = script.Body.Split('\n');
        Assert.All(lines[..8], line => Assert.Equal("", line.Trim()));
        Assert.Equal("var room = 1;", lines[8].Trim());
    }

    [Fact]
    public void Parse_LoadedScript_ContributesItsUsingLines()
    {
        _temp.WriteFile("scenarios/editor.csx", EditorScript);
        var path = _temp.WriteFile("scenarios/mop.csx", "#load \"editor.csx\"\nvar room = 1;\n");

        var script = ScriptSource.Parse(path);

        Assert.Equal(["using Verse;", "using static DevHarness.Scripting.Script;"], script.Usings.Select(u => u.Text));
        Assert.Empty(script.HelperFiles);
    }

    [Fact]
    public void Parse_MissingEditorFile_IsSkipped()
    {
        var path = _temp.WriteFile("scenarios/mop.csx", $"#load \"{EditorSupport.FileName}\"\nvar room = 1;\n");

        var script = ScriptSource.Parse(path);

        Assert.Empty(script.HelperFiles);
    }

    [Fact]
    public void Parse_LoadedScriptWithCode_Throws()
    {
        _temp.WriteFile("scenarios/setup.csx", "var room = 1;\n");
        var path = _temp.WriteFile("scenarios/mop.csx", "#load \"setup.csx\"\n");

        Assert.Throws<ScenarioException>(() => ScriptSource.Parse(path));
    }

    [Theory]
    [InlineData("// @teleport home")]
    [InlineData("#mod \"../MopMod/mod\"")]
    public void Parse_UnknownDirective_Throws(string directive)
    {
        var path = _temp.WriteFile("broken.csx", directive + "\n");

        Assert.Throws<ScenarioException>(() => ScriptSource.Parse(path));
    }
}
