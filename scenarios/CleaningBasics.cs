namespace Scenarios;

/// <summary>A colonist with only Cleaning enabled cleans blood, dirt and sand out of a small wooden room.</summary>
public sealed class CleaningBasics : HarnessScenario
{
    public override async Task Run()
    {
        // A small wooden room with one door
        Clock.Speed = TimeSpeed.Paused;
        var room = Build.Room()
            .CenteredOn(World.Center)
            .Size(11, 9)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Build();

        // A hard-working colonist who only cleans
        var cleaner = Spawn.Colonist()
            .WithName("Cleaner")
            .Inside(room)
            .Doing(WorkTypeDefOf.Cleaning)
            .WithTrait(TraitDefOf.Industriousness, degree: 1)
            .Spawn();
        Colonists.RemoveAllExcept(cleaner);

        // Blood, dirt and sand spread over the floor
        ThingDef[] mess = [ThingDefOf.Filth_Blood, ThingDefOf.Filth_Dirt, CleaningDefOf.Filth_Sand];
        room.Scatter(ThingDefOf.Filth_Blood, 8, seed: 1);
        room.Scatter(ThingDefOf.Filth_Dirt, 8, seed: 2);
        room.Scatter(CleaningDefOf.Filth_Sand, 8, seed: 3);
        var before = room.Filth(mess).Count;
        Note("filthBefore", before);

        // The mess before anyone cleans
        Camera.Frame(room);
        await Shot("before");

        // The cleaner starts cleaning
        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth(mess).Count < before, timeoutSeconds: 120);
        Check(cleaner.CurrentJob() == JobDefOf.Clean);

        // The cleaner keeps going until little is left
        await Until(() => room.Filth(mess).Count <= 4, timeoutSeconds: 150);
        Clock.Speed = TimeSpeed.Paused;

        // What is left once the cleaner is done
        Note("filthAfter", room.Filth(mess).Count);
        Note("sandAfter", room.Filth(CleaningDefOf.Filth_Sand).Count);
        await Shot("after");
    }
}
