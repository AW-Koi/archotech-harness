using ArchotechHarness.Scripting;
using RimWorld;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class IdentityTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly RoyalTitleDef Knight = new() { defName = "ArchoTest_Knight" };
    private static readonly PreceptDef Leader = new() { defName = "ArchoTest_Leader" };

    [Fact]
    public void GrantTitle_WithoutRoyalty_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.GrantTitle(Knight));
    }

    [Fact]
    public void AssignRole_WithoutAnIdeoligion_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.AssignRole(Leader));
    }

    [Fact]
    public void Relate_ToItself_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.Relate(pawn, new PawnRelationDef { defName = "ArchoTest_Spouse" }));
    }

    [Fact]
    public void SetOpinionOf_Itself_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.SetOpinionOf(pawn, 50));
    }

    [Theory]
    [InlineData(-101)]
    [InlineData(101)]
    public void SetOpinionOf_BeyondTheOpinionScale_Throws(int opinion)
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.SetOpinionOf(new Pawn(), opinion));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void ColonistSpawn_AgedBelowZero_Throws(float years)
    {
        var colonist = Spawn.Colonist().At(Cell).Aged(years);

        Assert.Throws<HarnessException>(() => colonist.Spawn());
    }

    [Theory]
    [InlineData(0f, DevelopmentalStage.Baby)]
    [InlineData(1f, DevelopmentalStage.Baby)]
    [InlineData(2.9f, DevelopmentalStage.Baby)]
    [InlineData(3f, DevelopmentalStage.Child)]
    [InlineData(12.9f, DevelopmentalStage.Child)]
    [InlineData(13f, DevelopmentalStage.Adult)]
    [InlineData(70f, DevelopmentalStage.Adult)]
    public void LifeStage_ForAnAge_IsTheHumanStageAtThatAge(float years, DevelopmentalStage expected)
    {
        Assert.Equal(expected, LifeStage.ForAge(years));
    }
}
