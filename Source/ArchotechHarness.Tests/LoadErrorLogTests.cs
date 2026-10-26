using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class LoadErrorLogTests
{
    private const string MissingCategory = "Could not resolve cross-reference: No Verse.KeyBindingCategoryDef named Misc found";
    private const string DuplicateSteel = "Mod Ludeon.RimWorld has multiple Verse.ThingDefs named Steel. Skipping.";
    private const string ScenarioError = "Exception ticking Janitor";

    [Fact]
    public void LoadErrorLog_ErrorsAfterTheFirstRead_AreLeftOut()
    {
        var log = new LoadErrorLog(capacity: 10);
        log.Add(MissingCategory);
        log.Add(DuplicateSteel);
        log.Close();

        log.Add(ScenarioError);

        Assert.Equal([MissingCategory, DuplicateSteel], log.Close());
    }

    [Fact]
    public void LoadErrorLog_BeyondItsCapacity_KeepsTheFirstErrors()
    {
        var log = new LoadErrorLog(capacity: 2);

        log.Add(MissingCategory);
        log.Add(DuplicateSteel);
        log.Add(ScenarioError);

        Assert.Equal([MissingCategory, DuplicateSteel], log.Close());
    }
}
