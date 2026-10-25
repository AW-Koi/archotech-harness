// A colonist with a mop and only Cleaning enabled cleans blood and dirt out of a small wooden room.
// @mod "../../JanitorsCloset/mod"
// @build "dotnet build Source/JanitorsCloset/JanitorsCloset.csproj --source https://api.nuget.org/v3/index.json" in "../../JanitorsCloset"
#load ".rwh-editor.csx"
using JanitorsCloset.Defs;

string[] mess = ["Filth_Blood", "Filth_Dirt"];

Clock.Speed = TimeSpeed.Paused;
var room = Build.Room(World.Center, width: 11, height: 9, floor: "WoodPlankFloor", door: Rot4.South);

var mopper = Spawn.Colonist("Mopper", inside: room, work: [WorkTypeDefOf.Cleaning], traits: ["Industriousness:1"]);
mopper.Equip(JanitorDefOf.Janitor_Mop.defName);
Colonists.RemoveAllExcept(mopper);

room.Scatter("Filth_Blood", 12, seed: 1);
room.Scatter("Filth_Dirt", 12, seed: 2);
var before = room.Filth(mess).Count;
Note("filthBefore", before);

Camera.Frame(room);
await Shot("before");

Clock.Speed = TimeSpeed.Superfast;
await Until(() => room.Filth(mess).Count < before, timeoutSeconds: 120);
Check(mopper.CurrentJob() == "Clean");

await Until(() => room.Filth(mess).Count <= 4, timeoutSeconds: 150);
Clock.Speed = TimeSpeed.Paused;

Note("filthAfter", room.Filth(mess).Count);
Note("mopperGear", mopper.equipment.Primary?.def.defName);
Note("mopMarks", World.Filth(room.Interior, JanitorDefOf.Janitor_MopMark.defName).Count);
await Shot("after");
