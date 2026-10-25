using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class BlueprintTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly ThingDef Mop = new() { defName = "ArchoTest_Mop", category = ThingCategory.Item };
    private static readonly ThingDef Stove = new() { defName = "ArchoTest_Stove", category = ThingCategory.Building, thingClass = typeof(RimWorld.Building_WorkTable) };

    [Fact]
    public void ThingSpawn_AsABlueprintOfAnItem_Throws()
    {
        var mop = Spawn.Thing(Mop).AsBlueprint().At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_AsABlueprintGivenToAPawn_Throws()
    {
        var stove = Spawn.Thing(Stove).AsBlueprint().GivenTo(new Pawn());

        Assert.Throws<HarnessException>(() => stove.Spawn());
    }

    [Fact]
    public void ThingSpawn_AsABlueprintWithStartingState_Throws()
    {
        var stove = Spawn.Thing(Stove).AsBlueprint().WithBill(new RecipeDef { defName = "ArchoTest_Soup" }).At(Cell);

        Assert.Throws<HarnessException>(() => stove.Spawn());
    }

    [Fact]
    public void LineBuild_AsABlueprintOfSomethingThatIsNotABuilding_Throws()
    {
        var line = Build.Line(Mop, Cell, Cell + new IntVec3(4, 0, 0)).AsBlueprint();

        Assert.Throws<HarnessException>(() => line.Build());
    }
}
