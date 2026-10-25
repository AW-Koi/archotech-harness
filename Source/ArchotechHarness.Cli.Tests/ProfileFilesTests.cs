using System.Xml.Linq;

namespace ArchotechHarness.Cli.Tests;

public sealed class ProfileFilesTests : IDisposable
{
    private const string GameVersion = "1.7.4985 rev1269";

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ModsConfig_WithLoadOrder_ListsActiveModsInOrder()
    {
        var path = _temp.Combine("ModsConfig.xml");

        ProfileFiles.WriteModsConfig(path, ["brrainz.harmony", "ludeon.rimworld", "terraincognita.archotechharness"], GameVersion);

        var activeMods = XDocument.Load(path).Root!.Element("activeMods")!.Elements("li").Select(e => e.Value);
        Assert.Equal(["brrainz.harmony", "ludeon.rimworld", "terraincognita.archotechharness"], activeMods);
    }

    [Fact]
    public void ModsConfig_AnyLoadOrder_MarksEveryExpansionAsKnown()
    {
        var path = _temp.Combine("ModsConfig.xml");

        ProfileFiles.WriteModsConfig(path, ["ludeon.rimworld"], GameVersion);

        var known = XDocument.Load(path).Root!.Element("knownExpansions")!.Elements("li").Select(e => e.Value);
        Assert.Equal(Dlc.All.Select(d => d.PackageId), known);
    }

    [Fact]
    public void Prefs_ForDevSession_EnablesDevModeInAWindowThatKeepsRunningUnfocused()
    {
        var path = _temp.Combine("Prefs.xml");

        ProfileFiles.WritePrefs(path, new WindowSize(1600, 900));

        var prefs = XDocument.Load(path).Root!;
        Assert.Equal("True", prefs.Element("devMode")!.Value);
        Assert.Equal("False", prefs.Element("fullscreen")!.Value);
        Assert.Equal("True", prefs.Element("runInBackground")!.Value);
        Assert.Equal("1600", prefs.Element("screenWidth")!.Value);
        Assert.Equal("900", prefs.Element("screenHeight")!.Value);
    }
}
