using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class PathCellsTests
{
    [Fact]
    public void Between_TwoCellsOneWide_RunsAlongXThenAlongZ()
    {
        var cells = PathCells.Between(new IntVec3(0, 0, 0), new IntVec3(3, 0, 2), width: 1);

        Assert.Equal(
            [new IntVec3(0, 0, 0), new IntVec3(1, 0, 0), new IntVec3(2, 0, 0), new IntVec3(3, 0, 0), new IntVec3(3, 0, 1), new IntVec3(3, 0, 2)],
            cells.Order(CellOrder.Instance));
    }

    [Fact]
    public void Between_TwoWide_CoversBothLegsAndTheCornerOnce()
    {
        var cells = PathCells.Between(new IntVec3(0, 0, 0), new IntVec3(4, 0, 4), width: 2);

        Assert.Contains(new IntVec3(0, 0, 1), cells);
        Assert.Contains(new IntVec3(5, 0, 1), cells);
        Assert.Contains(new IntVec3(5, 0, 4), cells);
        Assert.Equal(cells.Count, cells.Distinct().Count());
    }

    [Fact]
    public void Between_ACellAndItself_IsThatCell()
    {
        Assert.Equal([new IntVec3(2, 0, 2)], PathCells.Between(new IntVec3(2, 0, 2), new IntVec3(2, 0, 2), width: 1));
    }
}
