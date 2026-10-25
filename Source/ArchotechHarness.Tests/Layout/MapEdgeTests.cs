using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Tests.Layout;

public sealed class MapEdgeTests
{
    private static readonly IntVec3 MapSize = new(100, 1, 80);

    [Theory]
    [InlineData(0, 50, 77)]
    [InlineData(1, 97, 40)]
    [InlineData(2, 50, 2)]
    [InlineData(3, 2, 40)]
    public void Entry_OnEachSide_IsCentredOnThatEdgeAndInset(int side, int x, int z)
    {
        Assert.Equal(new IntVec3(x, 0, z), MapEdge.Entry(new Rot4(side), MapSize, inset: 2));
    }

    [Theory]
    [InlineData(0, 30, 50)]
    [InlineData(1, 50, 30)]
    [InlineData(2, 30, 10)]
    [InlineData(3, 10, 30)]
    public void Toward_ADirection_IsThatFarFromTheTarget(int side, int x, int z)
    {
        Assert.Equal(new IntVec3(x, 0, z), MapEdge.Toward(new IntVec3(30, 0, 30), new Rot4(side), 20, MapSize));
    }

    [Fact]
    public void Toward_BeyondTheMap_StopsAtTheEdge()
    {
        Assert.Equal(new IntVec3(30, 0, 79), MapEdge.Toward(new IntVec3(30, 0, 30), Rot4.North, 500, MapSize));
    }
}
