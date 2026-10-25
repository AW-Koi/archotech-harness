namespace Scenarios;

/// <summary>A colonist with only Cleaning enabled cleans blood, dirt and sand out of a small wooden room.</summary>
public sealed class CleaningBasics : HarnessScenario
{
    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        var room = Build.Room(World.Center, width: 11, height: 9, floor: TerrainDefOf.WoodPlankFloor, door: Rot4.South);

        var cleaner = Spawn.Colonist("Cleaner", inside: room, work: [WorkTypeDefOf.Cleaning],
            traits: [new Trait(TraitDefOf.Industriousness, degree: 1)]);
        Colonists.RemoveAllExcept(cleaner);

        ThingDef[] mess = [ThingDefOf.Filth_Blood, ThingDefOf.Filth_Dirt, CleaningDefOf.Filth_Sand];
        room.Scatter(ThingDefOf.Filth_Blood, 8, seed: 1);
        room.Scatter(ThingDefOf.Filth_Dirt, 8, seed: 2);
        room.Scatter(CleaningDefOf.Filth_Sand, 8, seed: 3);
        var before = room.Filth(mess).Count;
        Note("filthBefore", before);

        Camera.Frame(room);
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth(mess).Count < before, timeoutSeconds: 120);
        Check(cleaner.CurrentJob() == JobDefOf.Clean);

        await Until(() => room.Filth(mess).Count <= 4, timeoutSeconds: 150);
        Clock.Speed = TimeSpeed.Paused;

        Note("filthAfter", room.Filth(mess).Count);
        Note("sandAfter", room.Filth(CleaningDefOf.Filth_Sand).Count);
        await Shot("after");
    }
}
