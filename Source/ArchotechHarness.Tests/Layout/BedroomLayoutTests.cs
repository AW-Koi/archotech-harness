using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class BedroomLayoutTests
{
    private static readonly IntVec2 SingleBed = new(1, 2);
    private static readonly IntVec2 DoubleBed = new(2, 2);
    private static readonly IntVec2 Dresser = new(2, 1);
    private static readonly CellRect Bounds = new(0, 0, 9, 8);
    private static readonly IntVec3 SouthDoor = new(4, 0, 0);

    [Fact]
    public void Plan_DoorOnTheSouthWall_PutsTheBedsHeadAgainstTheNorthWallFacingTheDoor()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        var plan = BedroomLayout.Plan(grid, SingleBed, Dresser);

        Assert.Equal(Rot4.South, plan.Bed!.Rotation);
        Assert.Equal(new CellRect(4, 5, 1, 2), plan.Bed.Rect);
    }

    [Fact]
    public void Plan_DoorOnTheEastWall_PutsTheBedsHeadAgainstTheWestWall()
    {
        var grid = new RoomGrid(Bounds, [new IntVec3(8, 0, 3)], [], []);

        var plan = BedroomLayout.Plan(grid, SingleBed, Dresser);

        Assert.Equal(Rot4.East, plan.Bed!.Rotation);
        Assert.Equal(grid.Interior.minX, plan.Bed.Rect.minX);
    }

    [Fact]
    public void Plan_ADoubleBed_GetsAnEndTableOnEachSideOfItsHead()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        var plan = BedroomLayout.Plan(grid, DoubleBed, Dresser);

        Assert.Equal(new CellRect(3, 5, 2, 2), plan.Bed!.Rect);
        Assert.Equal([new IntVec3(2, 0, 6), new IntVec3(5, 0, 6)], plan.EndTables.Select(t => t.Cell).Order(CellOrder.Instance));
    }

    [Theory]
    [InlineData(9, 8, 4, 0)]
    [InlineData(9, 8, 8, 2)]
    [InlineData(6, 6, 0, 3)]
    [InlineData(7, 5, 3, 4)]
    [InlineData(5, 7, 2, 6)]
    public void Plan_AnyDoorPosition_KeepsFurnitureOffTheDoorwayAndTheBedsFoot(int width, int height, int doorX, int doorZ)
    {
        var grid = new RoomGrid(new CellRect(0, 0, width, height), [new IntVec3(doorX, 0, doorZ)], [], []);

        var plan = BedroomLayout.Plan(grid, SingleBed, Dresser);

        var furniture = plan.EndTables.Append(plan.Dresser!).Append(plan.Bed!).SelectMany(p => p.Rect.Cells).ToList();
        var foot = BedFoot(plan.Bed!);
        Assert.DoesNotContain(grid.Doorways.Single(), furniture);
        Assert.DoesNotContain(foot, furniture);
        Assert.True(grid.Interior.Contains(foot));
    }

    [Fact]
    public void Plan_WhenTheWallOppositeTheDoorIsTaken_StillPlacesTheBedAgainstAnotherWall()
    {
        var northRow = new CellRect(1, 6, 7, 1).Cells;
        var grid = new RoomGrid(Bounds, [SouthDoor], northRow, []);

        var plan = BedroomLayout.Plan(grid, SingleBed, Dresser);

        Assert.NotNull(plan.Bed);
        Assert.Contains(plan.Bed.Rect.Cells, grid.Interior.IsOnEdge);
    }

    [Fact]
    public void Plan_WithNoFreeFloor_PlacesNothing()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], new CellRect(1, 1, 7, 6).Cells, []);

        var plan = BedroomLayout.Plan(grid, SingleBed, Dresser);

        Assert.Null(plan.Bed);
        Assert.Empty(plan.EndTables);
        Assert.Null(plan.Dresser);
    }

    private static IntVec3 BedFoot(Placement bed) => bed.Rotation.AsInt switch
    {
        0 => new IntVec3(bed.Rect.minX, 0, bed.Rect.maxZ + 1),
        1 => new IntVec3(bed.Rect.maxX + 1, 0, bed.Rect.minZ),
        2 => new IntVec3(bed.Rect.minX, 0, bed.Rect.minZ - 1),
        _ => new IntVec3(bed.Rect.minX - 1, 0, bed.Rect.minZ),
    };
}
