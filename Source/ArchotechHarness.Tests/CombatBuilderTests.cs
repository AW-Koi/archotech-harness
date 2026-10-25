using ArchotechHarness.Scripting;
using RimWorld;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class CombatBuilderTests
{
    private static readonly IntVec3 Cell = new(10, 0, 10);
    private static readonly PawnKindDef Raider = new() { defName = "ArchoTest_Raider" };
    private static readonly ThingDef Mop = new() { defName = "ArchoTest_Mop", category = ThingCategory.Item };
    private static readonly ThingDef Bullet = new() { defName = "ArchoTest_Bullet", category = ThingCategory.Projectile, projectile = new ProjectileProperties() };
    private static readonly ThingDef Shell = new() { defName = "ArchoTest_Shell", category = ThingCategory.Item, projectileWhenLoaded = Bullet };
    private static readonly ThingDef Wall = new() { defName = "ArchoTest_Wall", category = ThingCategory.Building };

    [Fact]
    public void GroupSpawn_WithNoPawns_Throws()
    {
        var group = Spawn.Group(new Faction()).FromEdge(Rot4.North);

        Assert.Throws<HarnessException>(() => group.Spawn());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void GroupSpawn_FewerThanOneOfAKind_Throws(int count)
    {
        var group = Spawn.Group(new Faction()).Of(Raider, count).FromEdge(Rot4.North);

        Assert.Throws<HarnessException>(() => group.Spawn());
    }

    [Fact]
    public void GroupSpawn_BothAtACellAndFromAnEdge_Throws()
    {
        var group = Spawn.Group(new Faction()).Of(Raider, 2).At(Cell).FromEdge(Rot4.North);

        Assert.Throws<HarnessException>(() => group.Spawn());
    }

    [Fact]
    public void GroupSpawn_ArmedWithSomethingThatIsNotAWeapon_Throws()
    {
        var group = Spawn.Group(new Faction()).Of(Raider, 2).Armed(Mop).At(Cell);

        Assert.Throws<HarnessException>(() => group.Spawn());
    }

    [Fact]
    public void GroupSpawn_WithoutAFaction_Throws()
    {
        var group = Spawn.Group(null).Of(Raider, 2).At(Cell);

        Assert.Throws<HarnessException>(() => group.Spawn());
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-100f)]
    public void RaidSpawn_WithoutPositivePoints_Throws(float points)
    {
        var raid = Spawn.Raid(points);

        Assert.Throws<HarnessException>(() => raid.Spawn());
    }

    [Fact]
    public void ProjectileSpawn_WithoutATarget_Throws()
    {
        var shell = Spawn.Projectile(Shell).FromEdge(Rot4.North);

        Assert.Throws<HarnessException>(() => shell.Spawn());
    }

    [Fact]
    public void ProjectileSpawn_OfSomethingThatIsNotAProjectileOrShell_Throws()
    {
        var mop = Spawn.Projectile(Mop).At(Cell);

        Assert.Throws<HarnessException>(() => mop.Spawn());
    }

    [Fact]
    public void ProjectileSpawn_WithANegativeScatter_Throws()
    {
        var shell = Spawn.Projectile(Shell).At(Cell).Scatter(-1);

        Assert.Throws<HarnessException>(() => shell.Spawn());
    }

    [Fact]
    public void ProjectileSpawn_FewerThanOne_Throws()
    {
        var shell = Spawn.Projectile(Shell).At(Cell).Count(0);

        Assert.Throws<HarnessException>(() => shell.Spawn());
    }

    [Fact]
    public void TurretSpawn_OfSomethingThatIsNotATurret_Throws()
    {
        var wall = Spawn.Turret(Wall).At(Cell);

        Assert.Throws<HarnessException>(() => wall.Spawn());
    }

    [Fact]
    public void TurretSpawn_LoadedWithSomethingThatIsNotAShell_Throws()
    {
        var turret = Spawn.Turret(Wall).At(Cell).Loaded(Mop);

        Assert.Throws<HarnessException>(() => turret.Spawn());
    }

    [Fact]
    public void TurretSpawn_WithoutACell_Throws()
    {
        var turret = Spawn.Turret(Wall);

        Assert.Throws<HarnessException>(() => turret.Spawn());
    }

    [Fact]
    public void LineBuild_OfSomethingThatIsNotABuilding_Throws()
    {
        var line = Build.Line(Mop, Cell, Cell + new IntVec3(5, 0, 0));

        Assert.Throws<HarnessException>(() => line.Build());
    }

    [Fact]
    public void ColonistSpawn_ArmedWithSomethingThatIsNotAWeapon_Throws()
    {
        var colonist = Spawn.Colonist().At(Cell).Armed(Mop);

        Assert.Throws<HarnessException>(() => colonist.Spawn());
    }

    [Fact]
    public void ColonistSpawn_WearingSomethingThatIsNotApparel_Throws()
    {
        var colonist = Spawn.Colonist().At(Cell).Wearing(Mop);

        Assert.Throws<HarnessException>(() => colonist.Spawn());
    }
}
