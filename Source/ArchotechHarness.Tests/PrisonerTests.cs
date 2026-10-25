using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class PrisonerTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);

    [Fact]
    public void PrisonerSpawn_BothAtACellAndInsideARoom_Throws()
    {
        var prisoner = Spawn.Prisoner().At(Cell).Inside(new HarnessRoom(new CellRect(0, 0, 5, 5)));

        Assert.Throws<HarnessException>(() => prisoner.Spawn());
    }

    [Fact]
    public void PrisonerSpawn_FromNoFaction_Throws()
    {
        var prisoner = Spawn.Prisoner().From(null).At(Cell);

        Assert.Throws<HarnessException>(() => prisoner.Spawn());
    }

    [Fact]
    public void PrisonerSpawn_WithoutAPlace_Throws()
    {
        var prisoner = Spawn.Prisoner();

        Assert.Throws<HarnessException>(() => prisoner.Spawn());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void FastForward_WithoutPositiveTicks_Throws(int ticks)
    {
        Action fastForward = () => Script.FastForward(ticks);

        Assert.Throws<HarnessException>(fastForward);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void FastForwardUntil_WithoutAPositiveLimit_Throws(int maxTicks)
    {
        Action fastForward = () => Script.FastForwardUntil(() => true, maxTicks);

        Assert.Throws<HarnessException>(fastForward);
    }
}
