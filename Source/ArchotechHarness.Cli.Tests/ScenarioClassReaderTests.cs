namespace ArchotechHarness.Cli.Tests;

public sealed class ScenarioClassReaderTests : IDisposable
{
    private const string MopScenario = """
        using ArchotechHarness.Scripting;

        namespace MopMod.Scenarios;

        [Mod("../mod", Build = "dotnet build MopMod.csproj", BuildIn = "..")]
        [Dlc("biotech", "ideology")]
        [Window(1280, 720)]
        public sealed class MopsTheFloor : HarnessScenario
        {
            public override async Task Run() => await Until(() => true);
        }
        """;

    private const string MinimalScenario = """
        namespace MopMod.Scenarios
        {
            public class Smoke : ArchotechHarness.Scripting.HarnessScenario
            {
                public override Task Run() => Task.CompletedTask;
            }
        }
        """;

    private const string MopModProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <ArchoMod Include="../mod" Build="dotnet build MopMod.csproj" BuildIn=".." />
          </ItemGroup>
        </Project>
        """;

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Read_ScenarioWithFileScopedNamespace_FindsTheClassAndItsProject()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/Cleaning/MopsTheFloor.cs", MopScenario);

        var scenario = ScenarioClassReader.Read(file);

        Assert.Equal("MopMod.Scenarios.MopsTheFloor", scenario.TypeName);
        Assert.Equal(_temp.Combine("MopMod", "harness", "Scenarios.csproj"), scenario.ProjectFile);
    }

    [Fact]
    public void Read_ModAttribute_ResolvesPathsRelativeToTheScenarioFile()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/MopsTheFloor.cs", MopScenario);

        var scenario = ScenarioClassReader.Read(file);

        var mod = Assert.Single(scenario.Mods);
        Assert.Equal(_temp.Combine("MopMod", "mod"), mod.Directory);
        Assert.Equal("dotnet build MopMod.csproj", mod.Build!.Command);
        Assert.Equal(_temp.Combine("MopMod"), mod.Build.WorkingDirectory);
    }

    [Fact]
    public void Read_DlcAndWindowAttributes_SetTheProfile()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/MopsTheFloor.cs", MopScenario);

        var scenario = ScenarioClassReader.Read(file);

        Assert.Equal(["biotech", "ideology"], scenario.Dlcs);
        Assert.Equal(new WindowSize(1280, 720), scenario.Window);
    }

    [Fact]
    public void Read_ScenarioWithoutAttributes_UsesTheDefaultProfile()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        var scenario = ScenarioClassReader.Read(file);

        Assert.Equal("MopMod.Scenarios.Smoke", scenario.TypeName);
        Assert.Empty(scenario.Mods);
        Assert.Equal(new WindowSize(1600, 900), scenario.Window);
    }

    [Fact]
    public void Read_FileWithoutAScenarioClass_Throws()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/Helpers.cs", "public static class Helpers { }");

        Assert.Throws<ScenarioException>(() => ScenarioClassReader.Read(file));
    }

    [Fact]
    public void Read_FileOutsideAnyProject_Throws()
    {
        var file = _temp.WriteFile("loose/MopsTheFloor.cs", MopScenario);

        Assert.Throws<ScenarioException>(() => ScenarioClassReader.Read(file));
    }

    [Fact]
    public void Read_AttributeWithANonLiteralArgument_Throws()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project />");
        var file = _temp.WriteFile("MopMod/harness/Bad.cs", """
            [Mod(Paths.MopMod)]
            public class Bad : HarnessScenario { public override Task Run() => Task.CompletedTask; }
            """);

        Assert.Throws<ScenarioException>(() => ScenarioClassReader.Read(file));
    }

    [Fact]
    public void Read_ScenarioInASubfolder_GetsTheProjectModsResolvedFromTheProject()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", MopModProject);
        var file = _temp.WriteFile("MopMod/harness/Cleaning/Floors/Smoke.cs", MinimalScenario);

        var scenario = ScenarioClassReader.Read(file);

        var mod = Assert.Single(scenario.Mods);
        Assert.Equal(_temp.Combine("MopMod", "mod"), mod.Directory);
        Assert.Equal("dotnet build MopMod.csproj", mod.Build!.Command);
        Assert.Equal(_temp.Combine("MopMod"), mod.Build.WorkingDirectory);
    }

    [Fact]
    public void Read_ProjectModWithoutBuild_LoadsTheModWithoutBuildingIt()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", """
            <Project>
              <ItemGroup>
                <ArchoMod Include="../../Rugs/mod" />
              </ItemGroup>
            </Project>
            """);
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        var scenario = ScenarioClassReader.Read(file);

        var mod = Assert.Single(scenario.Mods);
        Assert.Equal(_temp.Combine("Rugs", "mod"), mod.Directory);
        Assert.Null(mod.Build);
    }

    [Fact]
    public void Read_ProjectModWithoutBuildIn_BuildsInTheModFolder()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", """
            <Project>
              <ItemGroup>
                <ArchoMod Include="../mod" Build="dotnet build" />
              </ItemGroup>
            </Project>
            """);
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        var scenario = ScenarioClassReader.Read(file);

        var mod = Assert.Single(scenario.Mods);
        Assert.Equal(_temp.Combine("MopMod", "mod"), mod.Build!.WorkingDirectory);
    }

    [Fact]
    public void Read_ClassModsAndProjectMods_MergesThemWithoutDuplicates()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", MopModProject);
        var file = _temp.WriteFile("MopMod/harness/Cleaning/Rugs.cs", """
            [Mod("../../mod")]
            [Mod("../../../Rugs/mod")]
            public class Rugs : HarnessScenario { public override Task Run() => Task.CompletedTask; }
            """);

        var scenario = ScenarioClassReader.Read(file);

        Assert.Equal([_temp.Combine("MopMod", "mod"), _temp.Combine("Rugs", "mod")], scenario.Mods.Select(m => m.Directory));
        Assert.Equal("dotnet build MopMod.csproj", scenario.Mods[0].Build!.Command);
    }

    [Fact]
    public void Read_ProjectWithoutModItems_GivesNoMods()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <Reference Include="MopMod" HintPath="../mod/1.7/Assemblies/MopMod.dll" Private="false" />
              </ItemGroup>
            </Project>
            """);
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        var scenario = ScenarioClassReader.Read(file);

        Assert.Empty(scenario.Mods);
    }

    [Theory]
    [InlineData("""<ArchoMod Build="dotnet build" />""")]
    [InlineData("""<ArchoMod Include="" />""")]
    [InlineData("""<ArchoMod Include="  " />""")]
    public void Read_ProjectModWithoutAnInclude_Throws(string item)
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", $"<Project><ItemGroup>{item}</ItemGroup></Project>");
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        Assert.Throws<ScenarioException>(() => ScenarioClassReader.Read(file));
    }

    [Fact]
    public void Read_ProjectThatIsNotXml_Throws()
    {
        _temp.WriteFile("MopMod/harness/Scenarios.csproj", "<Project><ItemGroup>");
        var file = _temp.WriteFile("MopMod/harness/Smoke.cs", MinimalScenario);

        Assert.Throws<ScenarioException>(() => ScenarioClassReader.Read(file));
    }
}
