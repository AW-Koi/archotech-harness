namespace Scenarios;

/// <summary>A colonist with a mop and only Cleaning enabled cleans blood and dirt out of a small wooden room.</summary>
[Mod("../../JanitorsCloset/mod",
    Build = "dotnet build Source/JanitorsCloset/JanitorsCloset.csproj --source https://api.nuget.org/v3/index.json",
    BuildIn = "../../JanitorsCloset")]
public sealed class JanitorBasics : HarnessScenario
{
    private static readonly string[] Mess = ["Filth_Blood", "Filth_Dirt"];

    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        var room = Build.Room(World.Center, width: 11, height: 9, floor: "WoodPlankFloor", door: Rot4.South);

        var mopper = Spawn.Colonist("Mopper", inside: room, work: [WorkTypeDefOf.Cleaning], traits: ["Industriousness:1"]);
        mopper.Equip("Janitor_Mop");
        Colonists.RemoveAllExcept(mopper);

        room.Scatter("Filth_Blood", 12, seed: 1);
        room.Scatter("Filth_Dirt", 12, seed: 2);
        var before = room.Filth(Mess).Count;
        Note("filthBefore", before);

        Camera.Frame(room);
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth(Mess).Count < before, timeoutSeconds: 120);
        Check(mopper.CurrentJob() == "Clean");

        await Until(() => room.Filth(Mess).Count <= 4, timeoutSeconds: 150);
        Clock.Speed = TimeSpeed.Paused;

        Note("filthAfter", room.Filth(Mess).Count);
        Note("mopperGear", mopper.equipment.Primary?.def.defName);
        Note("mopMarks", World.Filth(room.Interior, "Janitor_MopMark").Count);
        await Shot("after");
    }
}
