namespace ArchotechHarness.Cli.Tests;

public sealed class ModMetadataTests : IDisposable
{
    private const string GameVersion = "1.7.4985 rev1269";

    private const string TherapyAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Progression: Therapy</name>
          <packageId>ferny.ProgressionTherapy</packageId>
          <loadAfter>
            <li>Ludeon.RimWorld</li>
            <li>ferny.ProgressionEducation</li>
          </loadAfter>
          <modDependencies>
            <li>
              <packageId>ferny.ProgressionEducation</packageId>
              <displayName>Progression: Education</displayName>
            </li>
          </modDependencies>
        </ModMetaData>
        """;

    private const string VersionedAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Mop Mod</name>
          <packageId>TerraIncognita.MopMod</packageId>
          <modDependenciesByVersion>
            <v1.6>
              <li><packageId>Old.Bucket</packageId></li>
            </v1.6>
            <v1.7>
              <li><packageId>New.Bucket</packageId></li>
            </v1.7>
          </modDependenciesByVersion>
          <loadBefore>
            <li>Other.Broom</li>
          </loadBefore>
          <loadAfterByVersion>
            <v1.7>
              <li>New.Bucket</li>
            </v1.7>
          </loadAfterByVersion>
        </ModMetaData>
        """;

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Read_AboutListingDependencies_TakesThePackageIdFromTheTopLevel()
    {
        _temp.WriteFile("Therapy/About/About.xml", TherapyAbout);

        var metadata = ModMetadata.Read(_temp.Combine("Therapy"), GameVersion);

        Assert.Equal("ferny.progressiontherapy", metadata.PackageId);
        Assert.Equal("Progression: Therapy", metadata.Name);
        Assert.Equal(_temp.Combine("Therapy"), metadata.Directory);
    }

    [Fact]
    public void Read_ModDependencies_ListsTheirPackageIdsLowerCased()
    {
        _temp.WriteFile("Therapy/About/About.xml", TherapyAbout);

        var metadata = ModMetadata.Read(_temp.Combine("Therapy"), GameVersion);

        Assert.Equal(["ferny.progressioneducation"], metadata.Dependencies);
        Assert.Equal(["ludeon.rimworld", "ferny.progressioneducation"], metadata.LoadAfter);
        Assert.Empty(metadata.LoadBefore);
    }

    [Fact]
    public void Read_ByVersionLists_AddOnlyTheRunningGameVersion()
    {
        _temp.WriteFile("MopMod/About/About.xml", VersionedAbout);

        var metadata = ModMetadata.Read(_temp.Combine("MopMod"), GameVersion);

        Assert.Equal(["new.bucket"], metadata.Dependencies);
        Assert.Equal(["new.bucket"], metadata.LoadAfter);
        Assert.Equal(["other.broom"], metadata.LoadBefore);
    }

    [Fact]
    public void Read_FolderWithoutAbout_Throws()
    {
        Directory.CreateDirectory(_temp.Combine("NotAMod"));

        Assert.Throws<ScenarioException>(() => ModMetadata.Read(_temp.Combine("NotAMod"), GameVersion));
    }
}
