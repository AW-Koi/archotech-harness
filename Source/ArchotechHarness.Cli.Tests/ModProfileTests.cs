namespace ArchotechHarness.Cli.Tests;

public class ModProfileTests
{
    private const string MopModId = "terraincognita.mopmod";

    [Fact]
    public void LoadOrder_WithDlcAndModUnderTest_PutsHarmonyCoreDlcHarnessThenMods()
    {
        var order = ModProfile.LoadOrder(["biotech"], [MopModId]);

        Assert.Equal(
            ["brrainz.harmony", "ludeon.rimworld", "ludeon.rimworld.biotech", ModProfile.HarnessPackageId, MopModId],
            order);
    }

    [Fact]
    public void LoadOrder_DlcsInAnyOrder_FollowsReleaseOrder()
    {
        var order = ModProfile.LoadOrder(["odyssey", "royalty"], []);

        Assert.Equal(
            ["brrainz.harmony", "ludeon.rimworld", "ludeon.rimworld.royalty", "ludeon.rimworld.odyssey", ModProfile.HarnessPackageId],
            order);
    }

    [Fact]
    public void LoadOrder_UnknownDlc_Throws()
    {
        Assert.Throws<ScenarioException>(() => ModProfile.LoadOrder(["rimworld2"], []));
    }
}
