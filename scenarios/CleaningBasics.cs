namespace Scenarios;

/// <summary>A colonist with only Cleaning enabled cleans blood and dirt out of a small wooden room.</summary>
public sealed class CleaningBasics : HarnessScenario
{
    private static readonly string[] Mess = ["Filth_Blood", "Filth_Dirt"];

    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        var room = Build.Room(World.Center, width: 11, height: 9, floor: "WoodPlankFloor", door: Rot4.South);

        var cleaner = Spawn.Colonist("Cleaner", inside: room, work: [WorkTypeDefOf.Cleaning], traits: ["Industriousness:1"]);
        Colonists.RemoveAllExcept(cleaner);

        room.Scatter("Filth_Blood", 12, seed: 1);
        room.Scatter("Filth_Dirt", 12, seed: 2);
        var before = room.Filth(Mess).Count;
        Note("filthBefore", before);

        Camera.Frame(room);
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth(Mess).Count < before, timeoutSeconds: 120);
        Check(cleaner.CurrentJob() == "Clean");

        await Until(() => room.Filth(Mess).Count <= 4, timeoutSeconds: 150);
        Clock.Speed = TimeSpeed.Paused;

        Note("filthAfter", room.Filth(Mess).Count);
        await Shot("after");
    }
}
