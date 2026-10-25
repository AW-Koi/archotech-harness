namespace ArchotechHarness.Cli.Tests;

public sealed class RequiredModResolverTests : IDisposable
{
    private const string GameVersion = "1.7.4985 rev1269";
    private const string EducationId = "ferny.ProgressionEducation";

    private const string EducationAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Progression: Education</name>
          <packageId>ferny.ProgressionEducation</packageId>
          <modDependencies>
            <li><packageId>brrainz.harmony</packageId></li>
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
          <modDependencies>
            <li><packageId>brrainz.harmony</packageId></li>
            <li><packageId>Ludeon.RimWorld</packageId></li>
          </modDependencies>
        </ModMetaData>
        """;

    private const string TherapyAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Progression: Therapy</name>
          <packageId>ferny.ProgressionTherapy</packageId>
          <modDependencies>
            <li><packageId>ferny.ProgressionEducation</packageId></li>
          </modDependencies>
        </ModMetaData>
        """;

    private const string BucketAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Bucket</name>
          <packageId>TerraIncognita.Bucket</packageId>
          <modDependencies>
            <li><packageId>TerraIncognita.Mop</packageId></li>
          </modDependencies>
        </ModMetaData>
        """;

    private const string MopAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Mop</name>
          <packageId>TerraIncognita.Mop</packageId>
          <modDependencies>
            <li><packageId>TerraIncognita.Bucket</packageId></li>
          </modDependencies>
        </ModMetaData>
        """;

    private readonly TempDirectory _temp = new();
    private readonly RequiredModResolver _resolver;

    public RequiredModResolverTests()
    {
        Directory.CreateDirectory(_temp.Combine("Mods"));
        Directory.CreateDirectory(_temp.Combine("workshop"));
        _resolver = new RequiredModResolver([_temp.Combine("Mods"), _temp.Combine("workshop")], GameVersion);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Resolve_ModInTheModsFolder_FindsItsFolder()
    {
        _temp.WriteFile("Mods/VanillaExpandedFramework/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve(["OskarPotocki.VanillaFactionsExpanded.Core"]);

        var mod = Assert.Single(set.Mods);
        Assert.Equal(_temp.Combine("Mods", "VanillaExpandedFramework"), mod.Directory);
        Assert.Null(set.SkipReason);
    }

    [Fact]
    public void Resolve_ModInAWorkshopFolder_FindsItsFolder()
    {
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve(["oskarpotocki.vanillafactionsexpanded.core"]);

        var mod = Assert.Single(set.Mods);
        Assert.Equal(_temp.Combine("workshop", "2023507013"), mod.Directory);
    }

    [Fact]
    public void Resolve_ModInBothFolders_PrefersTheModsFolder()
    {
        _temp.WriteFile("Mods/VanillaExpandedFramework/About/About.xml", FrameworkAbout);
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve(["OskarPotocki.VanillaFactionsExpanded.Core"]);

        var mod = Assert.Single(set.Mods);
        Assert.Equal(_temp.Combine("Mods", "VanillaExpandedFramework"), mod.Directory);
    }

    [Fact]
    public void Resolve_ModNamedOnlyInAnotherModsDependencies_IsNotInstalled()
    {
        _temp.WriteFile("workshop/3753615736/About/About.xml", TherapyAbout);

        var set = _resolver.Resolve([EducationId]);

        Assert.Empty(set.Mods);
        Assert.Equal("ferny.ProgressionEducation is not installed", set.SkipReason);
    }

    [Fact]
    public void Resolve_ModWithDependencies_AddsThemTransitively()
    {
        _temp.WriteFile("workshop/3753615736/About/About.xml", TherapyAbout);
        _temp.WriteFile("workshop/3590863552/About/About.xml", EducationAbout);
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve(["ferny.ProgressionTherapy"]);

        Assert.Equal(
            ["ferny.progressiontherapy", "ferny.progressioneducation", "oskarpotocki.vanillafactionsexpanded.core"],
            set.Mods.Select(m => m.PackageId));
    }

    [Fact]
    public void Resolve_DlcDependency_EnablesTheDlc()
    {
        _temp.WriteFile("workshop/3590863552/About/About.xml", EducationAbout);
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve([EducationId]);

        Assert.Equal(["biotech"], set.Dlcs);
    }

    [Fact]
    public void Resolve_HarmonyAndCoreDependencies_AreLeftToTheHarness()
    {
        _temp.WriteFile("workshop/2023507013/About/About.xml", FrameworkAbout);

        var set = _resolver.Resolve(["OskarPotocki.VanillaFactionsExpanded.Core"]);

        Assert.Equal(["oskarpotocki.vanillafactionsexpanded.core"], set.Mods.Select(m => m.PackageId));
        Assert.Empty(set.Dlcs);
        Assert.Null(set.SkipReason);
    }

    [Fact]
    public void Resolve_MissingDependency_SkipsNamingTheModThatNeedsIt()
    {
        _temp.WriteFile("workshop/3590863552/About/About.xml", EducationAbout);

        var set = _resolver.Resolve([EducationId]);

        Assert.Equal("oskarpotocki.vanillafactionsexpanded.core is not installed (Progression: Education depends on it)", set.SkipReason);
    }

    [Fact]
    public void Resolve_DependencyCycle_ResolvesEachModOnce()
    {
        _temp.WriteFile("Mods/Bucket/About/About.xml", BucketAbout);
        _temp.WriteFile("Mods/Mop/About/About.xml", MopAbout);

        var set = _resolver.Resolve(["TerraIncognita.Bucket"]);

        Assert.Equal(["terraincognita.bucket", "terraincognita.mop"], set.Mods.Select(m => m.PackageId));
    }

    [Fact]
    public void Resolve_NoRequiredMods_GivesAnEmptySet()
    {
        var set = _resolver.Resolve([]);

        Assert.Empty(set.Mods);
        Assert.Empty(set.Dlcs);
        Assert.Null(set.SkipReason);
    }
}
