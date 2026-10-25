using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class BuilderValidationTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly ThingDef Mop = new() { defName = "ArchoTest_Mop", category = ThingCategory.Item };
    private static readonly ThingDef Potato = new() { defName = "ArchoTest_Potato", category = ThingCategory.Plant };
    private static readonly CellRect Bounds = new(10, 10, 7, 5);
    private static readonly CellRect Field = new(0, 0, 4, 4);
    private static readonly PawnKindDef Cat = new() { defName = "ArchoTest_Cat" };
    private static readonly TerrainDef Flagstone = new() { defName = "ArchoTest_Flagstone" };

    [Fact]
    public void RoomBuild_WithoutASize_Throws()
    {
        var room = Build.Room().CenteredOn(Cell);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Theory]
    [InlineData(2, 5)]
    [InlineData(5, 2)]
    public void RoomBuild_SmallerThanThreeByThree_Throws(int width, int height)
    {
        var room = Build.Room().CenteredOn(Cell).Size(width, height);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void ThingSpawn_WithoutAPlaceOrPawn_Throws()
    {
        var mop = Spawn.Thing(Mop);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ColonistSpawn_BothAtACellAndInsideARoom_Throws()
    {
        var room = new HarnessRoom(new CellRect(0, 0, 5, 5));
        var colonist = Spawn.Colonist().At(Cell).Inside(room);

        Assert.Throws<HarnessException>(() => colonist.Spawn());
    }

    [Fact]
    public void RoomBuild_CoveringAndSize_Throws()
    {
        var room = Build.Room().Covering(Bounds).Size(7, 5);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void RoomBuild_CoveringAndCenteredOn_Throws()
    {
        var room = Build.Room().Covering(Bounds).CenteredOn(Cell);

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void RoomBuild_CoveringLessThanThreeByThree_Throws()
    {
        var room = Build.Room().Covering(new CellRect(10, 10, 2, 5));

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Theory]
    [InlineData(13, 12)]
    [InlineData(13, 20)]
    [InlineData(10, 10)]
    [InlineData(16, 14)]
    public void RoomBuild_DoorOffTheWallOrOnACorner_Throws(int x, int z)
    {
        var room = Build.Room().Covering(Bounds).WithDoorAt(new IntVec3(x, 0, z));

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void RoomBuild_OneOfSeveralDoorsOffTheWall_Throws()
    {
        var room = Build.Room()
            .Covering(Bounds)
            .WithDoor(Rot4.South)
            .WithDoorAt(new IntVec3(13, 0, 12));

        Assert.Throws<HarnessException>(() => room.Build());
    }

    [Fact]
    public void ThingSpawn_FacingAnItem_Throws()
    {
        var mop = Spawn.Thing(Mop).Facing(Rot4.East).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void GrowingZoneBuild_WithoutAPlant_Throws()
    {
        var zone = Build.GrowingZone(Field);

        Assert.Throws<HarnessException>(() => zone.Build());
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.5f)]
    [InlineData(float.NaN)]
    public void GrowingZoneBuild_GrowthOutsideZeroToOne_Throws(float growth)
    {
        var zone = Build.GrowingZone(Field).Planting(Potato).Grown(growth);

        Assert.Throws<HarnessException>(() => zone.Build());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void ClockSkipToHour_OutsideTheDay_Throws(int hour)
    {
        Assert.Throws<HarnessException>(() => Clock.SkipToHour(hour));
    }

    [Fact]
    public void WorldCalm_WithoutARunningScript_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => World.Calm());
    }

    [Fact]
    public void ThingSpawn_FuelledWithoutAFuelTank_Throws()
    {
        var mop = Spawn.Thing(Mop).Fuelled().At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_ChargedWithoutABattery_Throws()
    {
        var mop = Spawn.Thing(Mop).Charged().At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_GrowingInSomethingThatIsNotAPlantGrower_Throws()
    {
        var mop = Spawn.Thing(Mop).Growing(Potato).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_WithABillOnSomethingThatIsNotAWorkTable_Throws()
    {
        var mop = Spawn.Thing(Mop).WithBill(new RecipeDef { defName = "ArchoTest_Soup" }).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ThingSpawn_HoldingOnSomethingThatIsNotStorage_Throws()
    {
        var mop = Spawn.Thing(Mop).Holding(Potato, 10).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void PlantSpawn_OfSomethingThatIsNotAPlant_Throws()
    {
        var mop = Spawn.Plant(Mop).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void PlantSpawn_WithoutACell_Throws()
    {
        var potato = Spawn.Plant(Potato);

        Assert.Throws<HarnessException>(() => potato.Spawn());
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.5f)]
    public void PlantSpawn_GrowthOutsideZeroToOne_Throws(float growth)
    {
        var potato = Spawn.Plant(Potato).At(Cell).Grown(growth);

        Assert.Throws<HarnessException>(() => potato.Spawn());
    }

    [Fact]
    public void AnimalSpawn_WithoutACell_Throws()
    {
        var cat = Spawn.Animal(Cat).Tame();

        Assert.Throws<HarnessException>(() => cat.Spawn());
    }

    [Fact]
    public void PathBuild_WithoutAFloor_Throws()
    {
        var path = Build.Path(Cell, Cell + new IntVec3(5, 0, 5));

        Assert.Throws<HarnessException>(() => path.Build());
    }

    [Fact]
    public void PathBuild_NarrowerThanOneCell_Throws()
    {
        var path = Build.Path(Cell, Cell + new IntVec3(5, 0, 5)).Of(Flagstone).Wide(0);

        Assert.Throws<HarnessException>(() => path.Build());
    }

    [Fact]
    public void BedroomBuild_WithSomethingThatIsNotABed_Throws()
    {
        var bedroom = Furnish.Bedroom(new HarnessRoom(Bounds)).Bed(Mop);

        Assert.Throws<HarnessException>(() => bedroom.Build());
    }

    [Fact]
    public void DiningBuild_WithSomethingThatIsNotATable_Throws()
    {
        var dining = Furnish.Dining(new HarnessRoom(Bounds), Bounds.ContractedBy(1)).Table(Mop);

        Assert.Throws<HarnessException>(() => dining.Build());
    }

    [Fact]
    public void DecorationBuild_WithNothingChosen_Throws()
    {
        var decoration = Decorate.Room(new HarnessRoom(Bounds));

        Assert.Throws<HarnessException>(() => decoration.Build());
    }
}
