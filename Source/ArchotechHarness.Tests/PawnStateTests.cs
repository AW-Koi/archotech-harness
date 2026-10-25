using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class PawnStateTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly ThingDef Mop = new() { defName = "ArchoTest_Mop", category = ThingCategory.Item };
    private static readonly HediffDef Flu = new() { defName = "ArchoTest_Flu" };
    private static readonly BodyPartDef Leg = new() { defName = "ArchoTest_Leg" };

    [Fact]
    public void Wound_WithAHediffThatIsNotAnInjury_Throws()
    {
        var pawn = new Pawn();

        Assert.Throws<HarnessException>(() => pawn.Wound(Flu, Leg, 10));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void Wound_WithoutPositiveSeverity_Throws(float severity)
    {
        var pawn = new Pawn();
        var gunshot = new HediffDef { defName = "ArchoTest_Gunshot", injuryProps = new InjuryProps() };

        Assert.Throws<HarnessException>(() => pawn.Wound(gunshot, Leg, severity));
    }

    [Fact]
    public void ThingSpawn_MedicalOnSomethingThatIsNotABed_Throws()
    {
        var mop = Spawn.Thing(Mop).Medical().At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_ForPrisonersOnSomethingThatIsNotABed_Throws()
    {
        var mop = Spawn.Thing(Mop).ForPrisoners().At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }
}
