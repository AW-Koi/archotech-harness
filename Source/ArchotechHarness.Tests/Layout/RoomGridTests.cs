using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class RoomGridTests
{
    private static readonly CellRect Bounds = new(0, 0, 9, 8);
    private static readonly IntVec3 SouthDoor = new(4, 0, 0);
    private static readonly CellRect SmallBounds = new(0, 0, 5, 5);
    private static readonly IntVec3 SmallSouthDoor = new(2, 0, 0);
    private static readonly IntVec2 Single = new(1, 1);

    [Fact]
    public void Doorways_DoorOnTheSouthWall_AreTheCellInsideIt()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        Assert.Equal([new IntVec3(4, 0, 1)], grid.Doorways);
    }

    [Fact]
    public void IsFree_ADoorway_IsFalse()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        Assert.False(grid.IsFree(new IntVec3(4, 0, 1)));
    }

    [Fact]
    public void DoorSide_DoorOnTheEastWall_IsEast()
    {
        var grid = new RoomGrid(Bounds, [new IntVec3(8, 0, 3)], [], []);

        Assert.Equal(Rot4.East, grid.DoorSide);
    }

    [Fact]
    public void WallSpots_ForASingleCellThing_AreFreeCellsAlongTheWallsFacingIntoTheRoom()
    {
        var blocked = new IntVec3(1, 0, 3);
        var grid = new RoomGrid(SmallBounds, [SmallSouthDoor], [blocked], []);

        var spots = grid.WallSpots(Single, rotatable: true).Select(s => (s.Cell, s.Rotation)).ToList();

        Assert.Contains((new IntVec3(2, 0, 3), Rot4.South), spots);
        Assert.Contains((new IntVec3(3, 0, 2), Rot4.West), spots);
        Assert.DoesNotContain(spots, s => s.Cell == blocked);
        Assert.DoesNotContain(spots, s => s.Cell == new IntVec3(2, 0, 1));
        Assert.DoesNotContain(spots, s => s.Cell == new IntVec3(2, 0, 2));
    }

    [Fact]
    public void FreeCorners_WithADoorwayInOne_SkipThatCorner()
    {
        var grid = new RoomGrid(Bounds, [new IntVec3(1, 0, 0)], [], []);

        Assert.Equal([new IntVec3(7, 0, 1), new IntVec3(7, 0, 6), new IntVec3(1, 0, 6)], grid.FreeCorners());
    }
}
