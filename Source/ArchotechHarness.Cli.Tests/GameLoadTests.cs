namespace ArchotechHarness.Cli.Tests;

public sealed class GameLoadTests
{
    private const string Group = "MopMod";

    private static readonly LoadError BaseGameError = new("Getting apparel graphic with undefined body type.", null, FromModUnderTest: false);

    [Fact]
    public void Failure_FourErrorsFromModsUnderTest_NamesTheFirstThree()
    {
        var load = new GameLoad(Group,
        [
            BaseGameError,
            new LoadError("Config error in MopMod_Mop: has null thingClass.", "Mop Mod", FromModUnderTest: true),
            new LoadError("Config error in MopMod_Bucket: has null thingClass.", "Mop Mod", FromModUnderTest: true),
            new LoadError("[Mop Mod] Patch operation Verse.PatchOperationAdd(/Defs) failed", "Mop Mod", FromModUnderTest: true),
            new LoadError("Config error in MopMod_Wringer: has null thingClass.", "Mop Mod", FromModUnderTest: true),
        ]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Equal(
            "The mods under test logged 4 errors while the game loaded: " +
            "Config error in MopMod_Mop: has null thingClass. | Config error in MopMod_Bucket: has null thingClass. | " +
            "[Mop Mod] Patch operation Verse.PatchOperationAdd(/Defs) failed (and 1 more). " +
            "Pass --allow-load-errors to run the scenarios anyway.",
            failure);
    }

    [Fact]
    public void Failure_MultiLineError_QuotesItsFirstLine()
    {
        var load = new GameLoad(Group,
            [new LoadError("[Mop Mod] Patch operation Verse.PatchOperationAdd(/Defs) failed\nfile: Patches/Mops.xml", "Mop Mod", FromModUnderTest: true)]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Equal(
            "The mods under test logged 1 error while the game loaded: [Mop Mod] Patch operation Verse.PatchOperationAdd(/Defs) failed. " +
            "Pass --allow-load-errors to run the scenarios anyway.",
            failure);
    }

    [Fact]
    public void Failure_OnlyErrorsFromOtherModsOrTheGame_IsNull()
    {
        var load = new GameLoad(Group, [BaseGameError, new LoadError("[Other Mod] Broken.", "Other Mod", FromModUnderTest: false)]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Null(failure);
    }

    [Fact]
    public void Failure_LoadErrorsAllowed_IsNull()
    {
        var load = new GameLoad(Group, [new LoadError("Config error in MopMod_Mop: has null thingClass.", "Mop Mod", FromModUnderTest: true)]);

        var failure = load.Failure(allowLoadErrors: true);

        Assert.Null(failure);
    }
}
