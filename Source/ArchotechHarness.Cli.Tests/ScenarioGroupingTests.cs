namespace ArchotechHarness.Cli.Tests;

public sealed class ScenarioGroupingTests : IDisposable
{
    private const string GameVersion = "1.7.4985 rev1269";
    private const string EducationId = "ferny.ProgressionEducation";
    private const string FrameworkId = "OskarPotocki.VanillaFactionsExpanded.Core";

    private const string EducationAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Progression: Education</name>
          <packageId>ferny.ProgressionEducation</packageId>
          <modDependencies>
            <li><packageId>OskarPotocki.VanillaFactionsExpanded.Core</packageId></li>
            <li><packageId>Ludeon.Rimworld.Biotech</packageId></li>
          </modDependencies>
        </ModMetaData>
        """;

    private const string FrameworkAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Vanilla Expanded Framework</name>
          <packageId>OskarPotocki.VanillaFactionsExpanded.Core</packageId>
        </ModMetaData>
        """;

    private static readonly ScenarioMod MopMod = new(Path.Combine("Mods", "MopMod", "mod"), null);
    private static readonly ScenarioMod BroomMod = new(Path.Combine("Mods", "BroomMod"), null);
    private static readonly WindowSize Window = new(1600, 900);

    private readonly TempDirectory _temp = new();
    private readonly RequiredModResolver _resolver;

    public ScenarioGroupingTests()
    {
        _temp.WriteFile("workshop/3590863552/About/About.xml", EducationAbout);
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);
        _resolver = new RequiredModResolver([_temp.Combine("workshop")], GameVersion);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Group_SameModsInAnyOrder_ShareOneBoot()
    {
        var first = Scenario("A", [MopMod, BroomMod], [], []);
        var second = Scenario("B", [BroomMod, MopMod], [], []);

        var groups = ScenarioGrouping.Group([first, second], _resolver);

        var group = Assert.Single(groups);
        Assert.Equal(["A", "B"], group.Scenarios.Select(s => s.TypeName));
    }

    [Fact]
    public void Group_DifferentDlcs_SplitIntoSeparateBootsInFirstSeenOrder()
    {
        var plain = Scenario("Plain", [MopMod], [], []);
        var biotech = Scenario("Biotech", [MopMod], ["biotech"], []);
        var plainAgain = Scenario("PlainAgain", [MopMod], [], []);

        var groups = ScenarioGrouping.Group([plain, biotech, plainAgain], _resolver);

        Assert.Equal(["MopMod", "MopMod+biotech"], groups.Select(g => g.Name));
        Assert.Equal(["Plain", "PlainAgain"], groups[0].Scenarios.Select(s => s.TypeName));
    }

    [Fact]
    public void Group_DifferentBiomes_SplitIntoSeparateBootsNamedForTheBiome()
    {
        var anywhere = Scenario("Anywhere", [MopMod], [], []);
        var forest = Scenario("Forest", [MopMod], [], []) with { Biome = "TemperateForest" };

        var groups = ScenarioGrouping.Group([anywhere, forest], _resolver);

        Assert.Equal(["MopMod", "MopMod+TemperateForest"], groups.Select(g => g.Name));
        Assert.Equal("TemperateForest", groups[1].Biome);
    }

    [Fact]
    public void Group_NoModsOrDlcs_IsNamedCore()
    {
        var groups = ScenarioGrouping.Group([Scenario("Smoke", [], [], [])], _resolver);

        Assert.Equal("core", Assert.Single(groups).Name);
    }

    [Fact]
    public void Group_DifferentRequiredMods_SplitIntoSeparateBoots()
    {
        var plain = Scenario("Plain", [MopMod], [], []);
        var withFramework = Scenario("WithFramework", [MopMod], [], [FrameworkId]);

        var groups = ScenarioGrouping.Group([plain, withFramework], _resolver);

        Assert.Equal(["MopMod", "MopMod+oskarpotocki.vanillafactionsexpanded.core"], groups.Select(g => g.Name));
        Assert.Equal(["oskarpotocki.vanillafactionsexpanded.core"], groups[1].RequiredMods.Select(m => m.PackageId));
    }

    [Fact]
    public void Group_RequiredModsInAnyCase_ShareOneBoot()
    {
        var first = Scenario("A", [MopMod], [], [FrameworkId]);
        var second = Scenario("B", [MopMod], [], [FrameworkId.ToUpperInvariant()]);

        var groups = ScenarioGrouping.Group([first, second], _resolver);

        Assert.Single(groups);
    }

    [Fact]
    public void Group_RequiredModNeedingADlc_SharesABootWithScenariosDeclaringThatDlc()
    {
        var implied = Scenario("Implied", [MopMod], [], [EducationId]);
        var declared = Scenario("Declared", [MopMod], ["biotech"], [EducationId]);

        var groups = ScenarioGrouping.Group([implied, declared], _resolver);

        var group = Assert.Single(groups);
        Assert.Equal(["biotech"], group.Dlcs);
        Assert.Equal(
            ["ferny.progressioneducation", "oskarpotocki.vanillafactionsexpanded.core"],
            group.RequiredMods.Select(m => m.PackageId));
    }

    [Fact]
    public void Group_RequiredModNotInstalled_IsSkippedWithTheReason()
    {
        var missing = Scenario("Missing", [MopMod], [], ["VanillaExpanded.VArmourE"]);

        var groups = ScenarioGrouping.Group([missing], _resolver);

        var group = Assert.Single(groups);
        Assert.Equal("VanillaExpanded.VArmourE is not installed", group.SkipReason);
    }

    private static ScenarioClass Scenario(string name, ScenarioMod[] mods, string[] dlcs, string[] requiredMods) =>
        new(name, Path.Combine("Mods", "harness", "Scenarios.cs"), Path.Combine("Mods", "harness", "Scenarios.csproj"), mods, dlcs, Window, requiredMods);
}
