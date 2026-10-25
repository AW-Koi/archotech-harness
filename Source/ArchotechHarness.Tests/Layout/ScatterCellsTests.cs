using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class ScatterCellsTests
{
    private static readonly IntVec3 Target = new(40, 0, 40);

    [Fact]
    public void Around_WithARadius_StaysWithinIt()
    {
        var cells = ScatterCells.Around(Target, radius: 4, count: 200, seed: 7);

        Assert.All(cells, cell => Assert.True((cell - Target).LengthHorizontalSquared <= 16));
    }

    [Fact]
    public void Around_TheSameSeed_PicksTheSameCells()
    {
        Assert.Equal(ScatterCells.Around(Target, 5, 10, seed: 3), ScatterCells.Around(Target, 5, 10, seed: 3));
    }

    [Fact]
    public void Around_ZeroRadius_IsTheTargetEveryTime()
    {
        Assert.Equal([Target, Target, Target], ScatterCells.Around(Target, 0, 3, seed: 1));
    }

    [Fact]
    public void Around_ACount_GivesThatManyCells()
    {
        Assert.Equal(6, ScatterCells.Around(Target, 3, 6, seed: 1).Count);
    }
}
