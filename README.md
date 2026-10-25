# Archotech Harness

Archotech Harness scripts RimWorld 1.7 end to end. You write a scenario in C#: it builds a colony, drives the game and checks the result. `archo` runs your scenarios in real games, apart from your own saves and settings, several at once if you like, and gives you a report with checks and screenshots.

## Quick start

You need:

- Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download).
- RimWorld 1.7. `archo` finds a Steam install by itself. For another install, set `RIMWORLD_DIR` to the folder that holds `Assembly-CSharp.dll`.
- The [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) mod, subscribed on Steam or in your Mods folder.

Clone the repo and run the sample scenario from its folder:

```sh
git clone https://github.com/AW-Koi/archotech-harness.git
cd archotech-harness
./archo run scenarios/CleaningBasics.cs
```

In PowerShell, use `.\archo.ps1` instead of `./archo`.

The first run builds `archo` and the harness mod, and later runs rebuild them when a file under `Source/` is newer than the last build, such as after a `git pull`. Set `ARCHO_REBUILD=1` to force a rebuild. The game opens, the scenario runs and the game closes. `archo` prints a JSON report and saves it, with the screenshots, in `.harness/runs/<scenario>-<time>/`.

Run every scenario in a folder with `./archo test scenarios`. That writes `report.html` and `run.json` to `.harness/test-runs/<time>/`, with each scenario's checks, notes, screenshots and a timeline of its steps, and a "Game load" section (`gameLoad` in `run.json`) with the errors each game logged while it loaded.

## Writing a scenario

A scenario is a class that derives from `HarnessScenario`. Its `Run` method runs inside the game once a fresh colony has loaded:

```csharp
namespace Scenarios;

public sealed class CleanerCleans : HarnessScenario
{
    public override async Task Run()
    {
        var room = Build.Room()
            .Size(9, 7)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Build();

        var cleaner = Spawn.Colonist()
            .Inside(room)
            .Doing(WorkTypeDefOf.Cleaning)
            .Spawn();
        Colonists.RemoveAllExcept(cleaner);

        room.Scatter(ThingDefOf.Filth_Blood, 10);
        var before = room.Filth().Count;
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth().Count < before, timeoutSeconds: 120);

        Check(cleaner.CurrentJob() == JobDefOf.Clean);
        await Shot("after");
    }
}
```

Save it in `scenarios/` and run it with `./archo run scenarios/CleanerCleans.cs`.

### Builders

`Build.Room()`, `Build.Stockpile(area)`, `Build.GrowingZone(area)`, `Spawn.Colonist()` and `Spawn.Thing(def)` return builders. Chain the options you need, then finish with `.Build()` or `.Spawn()`. Nothing happens until that call, and a builder you never finish fails the run.

Building is best-effort and later commands win. A building, plant, room, floor or zone replaces what was there before it, and a zone loses cells that a later building covers. Only programmer errors throw, such as a def of the wrong kind or a missing required option.

### Laying out a colony

- `World.Calm()` stops incidents (raids, visitors, mad animals, weather events), wake-ups, signal actions such as ambushes, and letters for the rest of the scenario, and keeps the weather clear. Call it first.
- `World.RemoveHostiles()` removes every pawn hostile to the player, dormant pawns such as ancient-danger sleepers, and strangers held in caskets and pods. It also removes what wakes or spawns threats: triggers, signal actions, mech cluster and other hostile buildings, hives and spawners. It returns how many things it removed.
- `Build.Clearing(rect)` makes an area open, walkable and unroofed. It removes buildings (ancient ones included), rock, plants, items and every pawn that isn't the player's, and turns water into soil.
- `Build.Room().Covering(rect)` sets a room's bounds, walls included, in place of `.CenteredOn` and `.Size`. Call `.WithDoor(side)` or `.WithDoorAt(cell)` once per door. A door cell off the walls, or on a corner, fails the build.
- `.Adjoining()` clears only inside the room's own bounds, so it can share a wall with a room built before it. A door already on that wall stays a door.
- `Spawn.Thing(def).Facing(rot).At(cell)` spawns a building exactly on the cell with that rotation, owned by the player. It replaces any buildings, items and plants in the way.
- `Build.Stockpile(rect)` makes a stockpile; `.Preset(preset)` picks the storage settings. `Build.GrowingZone(rect).Planting(plant)` makes a growing zone; `.Grown(0.8f)` sows the crop on every cell at that growth, laying soil where the ground can't grow it.
- `Build.Floor(rect, def)` lays a floor, such as a path or patio.
- `Clock.SkipToHour(9)` jumps the clock to that hour, without simulating the time between, so screenshots are in daylight. `Camera.Frame(rect)` frames any area.
- `Spawn.Thing(def)` also takes `.Fuelled()`, `.Charged()` (a battery), `.Growing(plant)` (a plant pot, fully grown), `.WithBill(recipe)` (a do-forever bill; repeatable) and `.Holding(def, count)` (stacks on a shelf; repeatable). Art always gets a title and description.
- `Spawn.Plant(def).At(cell).Grown(0.5f).Spawn()` plants a tree or flower in the ground, fully grown without `.Grown`. `Spawn.Animal(kind).At(cell).Tame().WithName("Whiskers").Spawn()` adds an animal; `.Tame()` makes it the player's.
- `Build.Path(from, to).Of(floor).Wide(2).Build()` lays an L-shaped path, along x then along z, one cell wide without `.Wide`.
- `Build.InWall(def, cell, facing)` replaces a wall cell with a cooler, vent or other wall-mounted building. It fails if the cell holds no wall.
- `Build.Room().Powered()` hides conduits in the walls, so powered things inside connect. `Build.Conduit(from, to)` lays a visible L-shaped conduit, such as from a generator. `World.Home(rect)` marks home area.

### Furnishing presets

Presets furnish a built room in one call, on free floor only. They never use the cell inside a door, interaction cells or the space beside a bed. Each piece tries its preferred spot, then corners, wall-side cells and any free floor. A piece that still doesn't fit is left out and noted in the report, such as "placed 0 of 1 plant pots, no free floor".

- `Furnish.Bedroom(room)` puts the bed's head against the wall opposite the door, an end table either side and a dresser. It takes `.Bed(def)`, `.MadeOf(stuff)`, `.Floor(def)`, `.Lamp()`, `.Plants(plant)`, `.Art(sculpture, stuff)` and `.Owner(pawns)`.
- `Furnish.Dining(room, area)` fills the area with tables and chairs facing them. It takes `.Table(def)` (`Table2x2c` without it) and `.MadeOf(stuff)`.
- `Decorate.Room(room)` takes `.PlantPots(plant)` (a pot in each free corner), `.Lamps()` (enough standing lamps to light the room) and `.Art(sculpture, stuff)`.

Each `.Build()` returns a `FurnishedRoom` with the `Things` it placed, `Bed` and `Of(def)`. Place work tables and other fittings before `Decorate.Room`, so it keeps their interaction cells clear.

### Waiting and checking

- `await Until(() => condition)` waits for a condition without freezing the game. It fails the run when the timeout passes.
- `await Ticks(n)` waits for game time. The game starts paused, so set `Clock.Speed` first.
- `await Reload()` saves the game, loads it back and waits until the map is playable. Look up pawns and things again afterwards, because the old references go stale.
- `Check(condition)` records a check and carries on. `Require(condition)` stops the scenario when it fails. The report shows the source text of each check.

### Game errors

A scenario fails when the game logs an error while it runs, even if every check passes. The report lists each distinct error with its count. Errors from loading the game are reported separately, under "Game load".

Put `[AllowGameErrors]` on the class to let it pass anyway, or `[AllowGameErrors("text")]` to accept only errors that contain that text (case-sensitive). Repeat the attribute for several texts.

### Screenshots and notes

`await Shot("name")` saves a screenshot. `Note("key", value)` adds a value to the report.

### Defs

Pass defs as objects, never as name strings. Use RimWorld's DefOf classes, your mod's own, or declare one in your scenarios project:

```csharp
[DefOf]
public static class MyScenarioDefOf
{
    public static ThingDef Filth_Sand;
}
```

The harness binds it when the scenario loads. A field whose def doesn't exist fails the run before it starts.

### Mods and DLCs

Declare the mods every scenario loads once, as `ArchoMod` items in the scenarios `.csproj`. Paths are relative to the `.csproj`.

```xml
<ItemGroup>
  <ArchoMod Include="../mod" Build="dotnet build" BuildIn=".." />
</ItemGroup>
```

| Item | Does |
| --- | --- |
| `<ArchoMod Include="../mod" />` | Loads the mod in that folder. Repeat it for several mods. |
| `<ArchoMod Include="../mod" Build="dotnet build" BuildIn=".." />` | Also builds the mod before the run. `BuildIn` defaults to the mod folder. |

The harness reads these items as plain XML, so it skips conditions, imports and `$(...)` properties.

Attributes on a class add to the project's mods for that scenario. Their paths are relative to the scenario file. A mod declared twice loads once. Scenarios with different mods, DLCs or biome run in separate games.

| Attribute | Does |
| --- | --- |
| `[Mod("../mod")]` | Loads the mod in that folder. Repeat it for several mods. |
| `[Mod("../mod", Build = "dotnet build", BuildIn = "..")]` | Also builds the mod before the run. |
| `[RequiresMod("ferny.ProgressionEducation")]` | Loads an installed mod by package id, with its dependencies. Repeat it for several mods. See [Compatibility scenarios](#compatibility-scenarios). |
| `[Dlc("biotech")]` | Activates DLCs: `royalty`, `ideology`, `biotech`, `anomaly`, `odyssey`. |
| `[Biome(nameof(BiomeDefOf.TemperateForest))]` | Starts the colony on that biome, on flat or small hills averaging 10 to 25C where the world has such a tile. A name that isn't a loaded biome colonies can settle fails the run. |

### Compatibility scenarios

A compatibility scenario runs your mod alongside another mod you have installed. Name the other mod by its package id, so the scenario works on any machine:

```csharp
[RequiresMod("ferny.ProgressionEducation")]
public sealed class EducationGatesTheSprayer : HarnessScenario
{
    public override async Task Run()
    {
        var sprayer = DefDatabase<ThingDef>.GetNamed("Janitor_HazmatSprayer");
        Check(sprayer.modExtensions.Any(e => e.GetType().Name == "ItemProficiencyRequirement"));
    }
}
```

- **Search:** `archo` matches the top-level `<packageId>` in each `About/About.xml`, ignoring case. It looks in the game's `Mods` folder first, then in `steamapps/workshop/content/294100` of every Steam library.
- **Dependencies:** it adds the mods in `<modDependencies>`, and in `<modDependenciesByVersion>` for the running game version, then their dependencies in turn. It leaves Harmony and Core to the harness, and activates a DLC dependency as `[Dlc]` does.
- **Load order:** Harmony, Core, DLCs and the harness load first. Each mod then loads after its dependencies and the mods in its `loadAfter`, and before the mods in its `loadBefore`. Otherwise required mods load before the mods under test.
- **Skipping:** when a required mod or one of its dependencies isn't installed, the scenario is skipped with the reason, such as `ferny.ProgressionEducation is not installed`. A skipped scenario doesn't fail `archo test`, and `archo run` reports it and exits with 0.
- **Workshop mods:** the game loads them through Steam. When Steam isn't running, `archo` links them into the `Mods` folder for the session. If the game still doesn't load a required mod, the boot fails.
- **Grouping:** scenarios with different required mods boot separate games.
- **Load errors:** errors from required mods show as warnings, as for the base game. Only the mods under test fail a group.

## Using it from your own mod

Keep your scenarios next to your mod, for example in `MyMod/harness/`. Add this `Scenarios.csproj` there:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ArchoHome Condition="'$(ArchoHome)' == ''">$(ARCHO_HOME)</ArchoHome>
    <ArchoHome Condition="'$(ArchoHome)' == '' And Exists('..\..\ArchotechHarness')">..\..\ArchotechHarness</ArchoHome>
  </PropertyGroup>
  <Import Project="$(ArchoHome)\build\ArchotechScenarios.props" Condition="Exists('$(ArchoHome)\build\ArchotechScenarios.props')" />

  <ItemGroup>
    <Reference Include="MyMod" HintPath="..\mod\1.7\Assemblies\MyMod.dll" Private="false" />
    <ArchoMod Include="../mod" />
  </ItemGroup>

  <Target Name="RequireArchotechHarness" BeforeTargets="ResolveAssemblyReferences" Condition="!Exists('$(ArchoHome)\build\ArchotechScenarios.props')">
    <Error Text="Archotech Harness not found. Clone https://github.com/AW-Koi/archotech-harness and set ARCHO_HOME to it." />
  </Target>
</Project>
```

1. Set the `ARCHO_HOME` environment variable to your harness clone. A clone in a sibling `ArchotechHarness` folder also works.
2. Run `./archo --help` in the harness once, so the harness mod is built.
3. Change the `Reference` to your mod's assembly, so scenarios can use its types and DefOf classes.
4. Point `ArchoMod` at your mod folder, then run the scenarios with `<harness>/archo test MyMod/harness`.

To edit scenarios in VS Code, open a folder, not a single file. Add `harness/Scenarios.csproj` to your mod's .sln, or open the `harness` folder.

Your mod can also expose its own checks to `archo send`. Add a class named `ArchotechProbes`; you don't need a reference to the harness:

```csharp
public static class ArchotechProbes
{
    public static int MopsOnMap() => Find.CurrentMap.listerThings.ThingsOfDef(MyModDefOf.Mop).Count;
}
```

Call it with `archo send probe '{"name":"MyMod.MopsOnMap"}'`.

## Commands

| Command | Does |
| --- | --- |
| `archo run FILE.cs` | Runs one scenario in a new game, then closes it. `--keep-open` leaves the game running; `--attach` uses a game that's already running. |
| `archo test [PATH]...` | Runs every scenario in the files, folders or projects given, and writes `report.html`. |
| `archo test --filter GLOB` | Runs only matching scenarios, such as `--filter "Mop*"`. Repeat it to match several patterns. |
| `archo test --retries N` | Retries a failed scenario up to N times. |
| `archo test --workers N` | Runs N games at once. `--workers auto` picks a number that fits your machine. |
| `archo test --allow-load-errors` | Runs the scenarios even when a mod under test logged errors while the game loaded. See [Load errors](#load-errors). |
| `archo test --video` | Records a video of each scenario into the report. It needs ffmpeg, on the PATH, installed with WinGet, or named by the `FFMPEG` environment variable. `--video-fps N` sets the frame rate (default 5). `run` takes it too. |
| `archo launch` | Starts a game and leaves it running for `send`. `--mod DIR` and `--dlc NAME` choose what it loads. |
| `archo send COMMAND ['{json}']` | Sends one command to the running game, such as `status`. |
| `archo status` | Lists your running games. |
| `archo stop` | Closes your games and restores your files. |
| `archo restore` | Restores your files after an interrupted run. |
| `archo clean` | Deletes old run folders now. `--keep N` and `--max-mb N` override the limits below; `--dry-run` lists what it would delete. |

Add `--wait` to `run` or `test` to wait for another game to close instead of stopping with an error. `archo test` exits with code 0 only when no scenario failed. Skipped scenarios count separately in the console, `run.json` and `report.html`.

Before each `run` or `test`, `archo` deletes the oldest folders in `.harness/runs/` and `.harness/test-runs/`. It keeps the 20 newest in each, up to 1024 MB per folder, and always the newest one. Set `ARCHO_KEEP_RUNS` (a number, or `all`) and `ARCHO_KEEP_MB` (`0` for no cap) to change that. It skips this while a game from this checkout is running, and never touches folders you write elsewhere with `--out`.

### Load errors

When `archo test` boots a game, it reads the errors the game logged while it loaded and set up its first colony, such as failed patch operations, config errors and exceptions. It blames an error on a mod under test when the error starts with the mod's name or package id in brackets, names one of its defs or def files, names a file in its folder, or names a type in one of its assembly namespaces. An error from a mod under test fails every scenario in that game's group, and the scenario error quotes the first three. Errors from the base game or other mods show as warnings in the console and the report. Pass `--allow-load-errors` to run the scenarios anyway.

## Reference

### Scenario API

| Member | Does |
| --- | --- |
| `Build.Room()` | Builds a walled room. Requires `.Size(w, h)` or `.Covering(rect)`; takes `.CenteredOn(cell)`, `.WithFloor(def)`, `.WithWalls(def)`, `.MadeOf(stuff)`, `.WithDoor(rot)`, `.WithDoorAt(cell)`, `.Adjoining()`, `.Powered()` and `.Roofed()`. Finish with `.Build()`. |
| `Build.Stockpile(rect)`, `Build.GrowingZone(rect)` | Makes a zone. A stockpile takes `.Preset(preset)`; a growing zone requires `.Planting(plant)` and takes `.Grown(growth)`. Finish with `.Build()`. |
| `Build.Floor(rect, def)`, `Build.Clearing(rect)` | Lays a floor, or clears an area to open ground. |
| `Build.Path(from, to)`, `Build.InWall(def, cell, facing)`, `Build.Conduit(from, to)` | Lays an L-shaped path, mounts a building in a wall, or lays a conduit. |
| `Furnish.Bedroom(room)`, `Furnish.Dining(room, area)`, `Decorate.Room(room)` | Furnishes or decorates a room. Finish with `.Build()`. |
| `Spawn.Plant(def)`, `Spawn.Animal(kind)` | Plants a tree or flower, or adds an animal. Finish with `.Spawn()`. |
| `Spawn.Colonist()` | Spawns a colonist. Takes `.WithName`, `.At(cell)` or `.Inside(room)`, `.Doing(workTypes)`, `.KeepingOtherWork()`, `.IncapableOf(tags)`, `.WithTrait`, `.WithSkill` and `.OfKind`. Finish with `.Spawn()`. |
| `Spawn.Thing(def)` | Makes a thing. Takes `.At(cell)` or `.GivenTo(pawn)`, `.MadeOf`, `.OfQuality`, `.Stack` and, for a building, `.Facing(rot)`, `.Fuelled()`, `.Charged()`, `.Growing(plant)`, `.WithBill(recipe)` and `.Holding(def, count)`. Finish with `.Spawn()`. |
| `Spawn.Filth(def, cell)` | Places filth. |
| `room.Scatter(def, count)`, `room.Filth(defs)` | Spreads filth over a room, or lists the filth in it. |
| `World.Map`, `World.Center`, `World.Filth`, `World.Things`, `World.Pawn` | The current map and queries over it. |
| `World.Calm()`, `World.RemoveHostiles()`, `World.Home(rect)` | Stops incidents and keeps clear weather, removes hostile and dormant pawns, or marks home area. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. |
| `pawn.CurrentJob()`, `pawn.SetWork`, `pawn.HasThought`, `pawn.Memories`, `pawn.FloatMenuAt(cell)` | Pawn shortcuts. |
| `Camera.Look`, `Camera.Frame(room)`, `Camera.Frame(rect)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick`, `Clock.SkipToHour(hour)` | Game speed and tick, and a jump to an hour of the day. |
| `Until`, `Ticks`, `Frames` | Waits. |
| `Check`, `Require`, `Note`, `Shot` | Records checks, values and screenshots. |

### Isolation and your files

Each game runs with its own save-data folder under `.harness/`, so it never reads or writes your saves, mod list or settings. RimWorld still writes `Player.log` to your data folder, so `archo` backs up your config and logs before the first game. It restores them, checking every hash, when the last game closes. A watchdog does the same if the game crashes or is killed, and `archo restore` covers anything that slips through. `archo` won't start while you're playing RimWorld, or while another `archo` on this machine has a game open.

## Further reading

- [docs/harness-commands.md](docs/harness-commands.md): the JSON commands behind `archo send`.
- [CONTRIBUTING.md](CONTRIBUTING.md): how the harness works, and how to build and test it.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
