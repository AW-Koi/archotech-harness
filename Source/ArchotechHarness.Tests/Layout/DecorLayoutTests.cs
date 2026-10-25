using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class DecorLayoutTests
{
    private const float LampRadius = 6f;
    private static readonly IntVec2 Single = new(1, 1);

    [Fact]
    public void Lamps_InALongHall_LightEveryCellWithinTheRadius()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 24, 7), [new IntVec3(12, 0, 0)], [], []);

        var lamps = DecorLayout.Lamps(grid, Single, LampRadius);

        Assert.True(lamps.Count > 1);
        Assert.All(grid.Interior.Cells, cell => Assert.Contains(lamps, lamp => lamp.Cell.DistanceTo(cell) <= LampRadius));
    }

    [Fact]
    public void Lamps_InASmallRoom_IsOne()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 6, 6), [new IntVec3(2, 0, 0)], [], []);

        Assert.Single(DecorLayout.Lamps(grid, Single, LampRadius));
    }

    [Fact]
    public void Art_InAnEmptyRoom_StandsAgainstAWallOffTheDoorway()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 7, 7), [new IntVec3(3, 0, 0)], [], []);

        var art = DecorLayout.Art(grid, Single, rotatable: false);

        Assert.NotEqual(new IntVec3(3, 0, 1), art.Cell);
        Assert.All(art.Rect.Cells, cell => Assert.True(grid.Interior.IsOnEdge(cell)));
    }

    [Fact]
    public void Art_WhenEveryWallCellIsTaken_StandsOnTheFreeFloor()
    {
        var wallCells = new CellRect(1, 1, 3, 3).EdgeCells.ToList();
        var grid = new RoomGrid(new CellRect(0, 0, 5, 5), [], wallCells, []);

        var art = DecorLayout.Art(grid, Single, rotatable: false);

        Assert.Equal(new IntVec3(2, 0, 2), art!.Cell);
    }

    [Fact]
    public void Art_WithNoFreeFloor_IsNull()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 5, 5), [], new CellRect(1, 1, 3, 3).Cells, []);

        Assert.Null(DecorLayout.Art(grid, Single, rotatable: false));
    }

    [Fact]
    public void Pots_WithEveryCornerTaken_GoOnAWallCell()
    {
        var corners = new[] { new IntVec3(1, 0, 1), new IntVec3(5, 0, 1), new IntVec3(5, 0, 5), new IntVec3(1, 0, 5) };
        var grid = new RoomGrid(new CellRect(0, 0, 7, 7), [], corners, []);

        var pot = Assert.Single(DecorLayout.Pots(grid, 1, new IntVec3(3, 0, 3)));
        Assert.True(grid.Interior.IsOnEdge(pot));
        Assert.DoesNotContain(pot, corners);
    }

    [Fact]
    public void Pots_WithNoFreeFloor_PlacesNothing()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 5, 5), [], new CellRect(1, 1, 3, 3).Cells, []);

        Assert.Empty(DecorLayout.Pots(grid, 1, new IntVec3(2, 0, 2)));
    }

    [Fact]
    public void Lamps_WithNoFreeFloor_PlacesNone()
    {
        var grid = new RoomGrid(new CellRect(0, 0, 5, 5), [], new CellRect(1, 1, 3, 3).Cells, []);

        Assert.Empty(DecorLayout.Lamps(grid, Single, LampRadius));
    }
}
