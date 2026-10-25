namespace ArchotechHarness.Cli.Tests;

public class SteamLibrariesTests
{
    private const string RimWorldAppId = "294100";

    private const string ThreeLibraries = """
        "libraryfolders"
        {
            "0"
            {
                "path"		"/steam/main"
                "label"		""
                "contentid"		"123"
                "totalsize"		"0"
                "apps"
                {
                    "228980"		"470440765"
                    "250820"		"5512424393"
                }
            }
            "1"
            {
                "path"		"/steam/library"
                "label"		"games"
                "apps"
                {
                    "294100"		"1830928711"
                    "1222680"		"9152043360"
                }
            }
            "2"
            {
                "path"		"Games\\Steam"
                "apps"
                {
                }
            }
        }
        """;

    [Fact]
    public void Parse_SeveralLibraries_ReadsEachPathWithItsApps()
    {
        var libraries = SteamLibraries.Parse(ThreeLibraries);

        Assert.Equal(["/steam/main", "/steam/library", @"Games\Steam"], libraries.Select(l => l.Path));
        Assert.Equal(["294100", "1222680"], libraries[1].AppIds);
        Assert.Empty(libraries[2].AppIds);
    }

    [Fact]
    public void LibraryWithApp_RimWorldInTheSecondLibrary_ReturnsThatLibrary()
    {
        var library = SteamLibraries.LibraryWithApp(ThreeLibraries, RimWorldAppId);

        Assert.Equal("/steam/library", library);
    }

    [Fact]
    public void LibraryWithApp_AppNotInstalled_ReturnsNull()
    {
        var library = SteamLibraries.LibraryWithApp(ThreeLibraries, "999999");

        Assert.Null(library);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsNoLibraries()
    {
        Assert.Empty(SteamLibraries.Parse(""));
    }
}
