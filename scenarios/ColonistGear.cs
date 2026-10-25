namespace Scenarios;

/// <summary>Things made to order: a steel knife a colonist equips, a parka it wears and a stack of steel on the floor.</summary>
public sealed class ColonistGear : HarnessScenario
{
    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        var room = Build.Room()
            .Size(7, 7)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .Roofed()
            .Build();

        var hauler = Spawn.Colonist()
            .WithName("Hauler")
            .Inside(room)
            .Doing(WorkTypeDefOf.Hauling)
            .Spawn();
        Colonists.RemoveAllExcept(hauler);

        var knife = Spawn.Thing(ThingDefOf.MeleeWeapon_Knife)
            .MadeOf(ThingDefOf.Steel)
            .OfQuality(QualityCategory.Good)
            .GivenTo(hauler)
            .Spawn();

        Spawn.Thing(ThingDefOf.Apparel_Parka)
            .GivenTo(hauler)
            .Spawn();

        var steel = Spawn.Thing(ThingDefOf.Steel)
            .Stack(40)
            .At(room.Center + IntVec3.East)
            .Spawn();

        Check(hauler.equipment.Primary == knife);
        Check(knife.Stuff == ThingDefOf.Steel);
        Check(knife.TryGetQuality(out var quality) && quality == QualityCategory.Good);
        Check(hauler.apparel.WornApparel.Any(a => a.def == ThingDefOf.Apparel_Parka));
        Check(steel.stackCount == 40);
        Check(room.Contains(steel));

        Camera.Frame(room);
        await Shot("gear");
    }
}
