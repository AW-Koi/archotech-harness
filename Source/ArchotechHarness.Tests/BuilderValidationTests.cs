using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class BuilderValidationTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly ThingDef Mop = new() { defName = "ArchoTest_Mop" };

    [Fact]
    public void RoomBuild_WithoutASize_Throws()
    {
        var room = Build.Room().CenteredOn(Cell);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Theory]
    [InlineData(2, 5)]
    [InlineData(5, 2)]
    public void RoomBuild_SmallerThanThreeByThree_Throws(int width, int height)
    {
        var room = Build.Room().CenteredOn(Cell).Size(width, height);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void ThingSpawn_WithoutAPlaceOrPawn_Throws()
    {
        var mop = Spawn.Thing(Mop);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ColonistSpawn_BothAtACellAndInsideARoom_Throws()
    {
        var room = new HarnessRoom(new CellRect(0, 0, 5, 5));
        var colonist = Spawn.Colonist().At(Cell).Inside(room);

        Assert.Throws<HarnessException>(() => colonist.Spawn());
    }
}
