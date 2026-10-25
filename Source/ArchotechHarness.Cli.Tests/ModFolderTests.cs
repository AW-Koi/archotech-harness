namespace ArchotechHarness.Cli.Tests;

public sealed class ModFolderTests : IDisposable
{
    private const string MopModAbout = """
        <?xml version="1.0" encoding="utf-8"?>
        <ModMetaData>
          <name>Mop Mod</name>
          <packageId>TerraIncognita.MopMod</packageId>
        </ModMetaData>
        """;

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void PackageId_FromAboutXml_IsLowerCased()
    {
        _temp.WriteFile("MopMod/About/About.xml", MopModAbout);

        var packageId = ModFolder.ReadPackageId(_temp.Combine("MopMod"));

        Assert.Equal("terraincognita.mopmod", packageId);
    }

    [Fact]
    public void PackageId_FolderWithoutAbout_Throws()
    {
        Directory.CreateDirectory(_temp.Combine("NotAMod"));

        Assert.Throws<ScenarioException>(() => ModFolder.ReadPackageId(_temp.Combine("NotAMod")));
    }

    [Fact]
    public void FindInstalled_ModUnderModsFolder_ReturnsItsFolder()
    {
        _temp.WriteFile("Mods/MopMod/About/About.xml", MopModAbout);

        var found = ModFolder.FindInstalled(_temp.Combine("Mods"), "terraincognita.mopmod");

        Assert.Equal(_temp.Combine("Mods", "MopMod"), found);
    }

    [Fact]
    public void FindInstalled_ModMissing_ReturnsNull()
    {
        Directory.CreateDirectory(_temp.Combine("Mods"));

        var found = ModFolder.FindInstalled(_temp.Combine("Mods"), "terraincognita.mopmod");

        Assert.Null(found);
    }
}
