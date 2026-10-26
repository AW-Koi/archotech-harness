namespace ArchotechHarness.Cli.Tests;

public sealed class GameLoadTests
{
    private const string Group = "MopMod";

    private const string MissingKeyBindingCategory =
        "Could not resolve cross-reference: No Verse.KeyBindingCategoryDef named Misc found to give to Verse.KeyBindingDef MopMod_Tool";

    private const string KeyPrefsCrash =
        "Exception from asynchronous event: System.NullReferenceException: Object reference not set to an instance of an object.\n" +
        "   at Verse.KeyPrefsData.ErrorCheck()\n   at Verse.PlayDataLoader.LoadAllPlayData(Boolean recovering)";

    private const string SecondDataLoad = "LoadDefs called with already existing def packages";

    private const string DuplicateSteel = "Mod Ludeon.RimWorld has multiple Verse.ThingDefs named Steel. Skipping.";

    private const string DuplicateWood = "Mod Ludeon.RimWorld has multiple Verse.ThingDefs named WoodLog. Skipping.";

    private const string UndefinedBodyType = "Getting apparel graphic with undefined body type.";

    private static readonly ModFingerprint MopMod = new("Mop Mod", "TerraIncognita.MopMod", [],
        new HashSet<string> { "MopMod_Tool" }, new HashSet<string>(), new HashSet<string>());

    private static readonly LoadError BaseGameError = new(UndefinedBodyType, null, FromModUnderTest: false);

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

    [Fact]
    public void SecondDataLoad_AfterTheFirstLoadFailed_CountsTheErrorsFromTheMarkerOn()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, KeyPrefsCrash, SecondDataLoad, DuplicateSteel, DuplicateWood], [MopMod]);

        Assert.Equal(3, load.SecondDataLoadErrors);
    }

    [Fact]
    public void SecondDataLoad_WhenTheFirstLoadSucceeded_CountsNoErrors()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, UndefinedBodyType], [MopMod]);

        Assert.Equal(0, load.SecondDataLoadErrors);
    }

    [Fact]
    public void Failure_ModUnderTestBrokeTheFirstDataLoad_ExplainsTheSecondLoadBeforeQuotingTheErrors()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, KeyPrefsCrash, SecondDataLoad, DuplicateSteel, DuplicateWood], [MopMod]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Equal(
            "Mop Mod broke the game's first data load, so the game loaded its data a second time on top of it and added every def twice; " +
            "3 errors come from that second load. " +
            "The mods under test logged 1 error while the game loaded: " + MissingKeyBindingCategory + ". " +
            "Pass --allow-load-errors to run the scenarios anyway.",
            failure);
    }

    [Fact]
    public void Failure_AnotherModBrokeTheFirstDataLoad_IsNull()
    {
        var load = GameLoad.FromLog(Group, [KeyPrefsCrash, SecondDataLoad, DuplicateSteel], [MopMod]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Null(failure);
    }

    [Fact]
    public void Summary_AnotherModBrokeTheFirstDataLoad_BlamesTheBaseGameOrAnotherModAndListsWarnings()
    {
        var load = GameLoad.FromLog(Group, [KeyPrefsCrash, SecondDataLoad, DuplicateSteel], [MopMod]);

        var summary = load.Summary(shownPerKind: 5);

        Assert.Equal(
        [
            "Game load for MopMod: 0 error(s) from mods under test, 3 from the base game or other mods.",
            "  The base game or another mod broke the game's first data load, so the game loaded its data a second time on top of it and added every def twice; " +
            "2 errors come from that second load.",
            "  Load warning (base game or another mod): Exception from asynchronous event: System.NullReferenceException: Object reference not set to an instance of an object.",
            "  Load warning (base game or another mod): " + SecondDataLoad,
            "  Load warning (base game or another mod): " + DuplicateSteel,
        ], summary);
    }

    [Fact]
    public void Summary_RepeatedAndSurplusErrors_ListsEachTextOnceWithItsCountAndCountsTheRest()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, MissingKeyBindingCategory, UndefinedBodyType, DuplicateSteel, DuplicateWood], [MopMod]);

        var summary = load.Summary(shownPerKind: 1);

        Assert.Equal(
        [
            "Game load for MopMod: 2 error(s) from mods under test, 3 from the base game or other mods.",
            "  Load error (Mop Mod): " + MissingKeyBindingCategory + " (2 times)",
            "  Load warning (base game or another mod): " + UndefinedBodyType,
            "  ...and 2 more; see the report.",
        ], summary);
    }

    [Fact]
    public void Failure_RepeatedError_QuotesItOnceWithItsCount()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, MissingKeyBindingCategory, MissingKeyBindingCategory], [MopMod]);

        var failure = load.Failure(allowLoadErrors: false);

        Assert.Equal(
            "The mods under test logged 3 errors while the game loaded: " + MissingKeyBindingCategory + " (3 times). " +
            "Pass --allow-load-errors to run the scenarios anyway.",
            failure);
    }

    [Fact]
    public void Summary_NoErrors_IsEmpty()
    {
        var load = GameLoad.FromLog(Group, [], [MopMod]);

        var summary = load.Summary(shownPerKind: 5);

        Assert.Empty(summary);
    }

    [Fact]
    public void CompactJson_ModUnderTestBrokeTheFirstDataLoad_ListsItsErrorsAndCountsTheRest()
    {
        var load = GameLoad.FromLog(Group, [MissingKeyBindingCategory, KeyPrefsCrash, SecondDataLoad, DuplicateSteel], [MopMod]);

        var json = load.ToCompactJson();

        Assert.Equal(
            """{"errors":4,"fromModsUnderTest":[{"text":"Could not resolve cross-reference: No Verse.KeyBindingCategoryDef named Misc found to give to Verse.KeyBindingDef MopMod_Tool","mod":"Mop Mod","count":1}]""" +
            ""","secondDataLoad":{"brokenBy":["Mop Mod"],"errors":2}}""",
            json.ToJsonString());
    }

    [Fact]
    public void CompactJson_NoSecondDataLoad_LeavesItOut()
    {
        var load = GameLoad.FromLog(Group, [UndefinedBodyType], [MopMod]);

        var json = load.ToCompactJson();

        Assert.Equal("""{"errors":1,"fromModsUnderTest":[]}""", json.ToJsonString());
    }
}
