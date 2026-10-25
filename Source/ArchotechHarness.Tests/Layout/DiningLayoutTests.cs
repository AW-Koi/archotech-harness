using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class DiningLayoutTests
{
    private static readonly IntVec2 SquareTable = new(2, 2);
    private static readonly CellRect Bounds = new(0, 0, 12, 8);
    private static readonly IntVec3 SouthDoor = new(6, 0, 0);

    [Fact]
    public void Plan_AreaForTwoTables_SeatsEachTableWithChairsFacingIt()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        var sets = DiningLayout.Plan(grid, new CellRect(2, 2, 8, 4), SquareTable, rotatable: false);

        Assert.Equal(2, sets.Count);
        Assert.All(sets, set => Assert.Equal(8, set.Chairs.Count));
        Assert.All(sets, set => Assert.All(set.Chairs, chair => Assert.True(set.Table.Rect.Contains(chair.Cell + chair.Rotation.FacingCell))));
    }

    [Fact]
    public void Plan_AreaTooSmallForATableAndItsChairs_PlacesNoTables()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        Assert.Empty(DiningLayout.Plan(grid, new CellRect(2, 2, 3, 3), SquareTable, rotatable: false));
    }

    [Fact]
    public void Plan_AreaReachingOutsideTheRoom_Throws()
    {
        var grid = new RoomGrid(Bounds, [SouthDoor], [], []);

        Assert.Throws<HarnessException>(() => DiningLayout.Plan(grid, new CellRect(0, 0, 6, 6), SquareTable, rotatable: false));
    }

    [Fact]
    public void Plan_AreaOverTheDoorway_LeavesTheDoorwayClearAndSeatsTheRest()
    {
        var doorUnderAChair = new IntVec3(7, 0, 0);
        var grid = new RoomGrid(Bounds, [doorUnderAChair], [], []);

        var sets = DiningLayout.Plan(grid, new CellRect(1, 1, 10, 4), SquareTable, rotatable: false);

        var used = sets.SelectMany(s => s.Chairs.Append(s.Table)).SelectMany(p => p.Rect.Cells).ToList();
        Assert.DoesNotContain(new IntVec3(7, 0, 1), used);
        Assert.Equal(2, sets.Count);
    }
}
