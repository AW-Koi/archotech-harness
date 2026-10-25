# Archotech Harness

Start RimWorld 1.7 from a script, set up a colony, drive the game, read results back as JSON and take screenshots. It's built for AI agents and for repeatable in-game checks of your own mods.

The harness has three parts:

- `archo`, a command-line tool that prepares a dev profile, launches the game straight into a quick-test map, runs scenarios and restores your files afterwards.
- A dev-only mod (`TerraIncognita.ArchotechHarness`) that listens on `127.0.0.1` and runs commands on the game's main thread.
- Scenarios: C# classes in a scenarios project that `archo` builds and runs inside the game.

## Requirements

- Windows, the .NET 10 SDK, and RimWorld 1.7 (the Waylith build).
- The Harmony mod (`brrainz.harmony`) with 1.7 support, from the Steam workshop or the Mods folder.
- Steam running, if Harmony comes from the workshop.

`archo` and the builds find RimWorld through Steam: they read the Steam install path from the registry, then the library in `libraryfolders.vdf` that holds app 294100. Set `RIMWORLD_DIR` to the folder that holds `Assembly-CSharp.dll` to use another install.

## Quick start

From Git Bash, in the repo folder:

```sh
./archo run scenarios/CleaningBasics.cs
```

From PowerShell:

```powershell
.\archo.ps1 run scenarios\CleaningBasics.cs
```

The first call builds the CLI and the mod. Set `ARCHO_REBUILD=1` to rebuild after you pull changes. A scenario run prints one JSON report line. Screenshots and the report go to `.harness/runs/<scenario>-<timestamp>/`, or to the folder you pass with `--out`.

To run every scenario in a folder or project:

```sh
./archo test scenarios
```

## What happens to your game files

- **Config:** the game runs with `-savedatafolder=<repo>/.harness/profile`, so it reads and writes a separate ModsConfig.xml, Prefs.xml and Saves folder. Your own config and saves stay as they are.
- **Logs:** RimWorld always writes `Player.log` to your real data folder. `archo` backs up `ModsConfig.xml`, `Prefs.xml`, `Player.log` and `Player-prev.log` before launch, then restores them and checks their SHA-256 hashes when the session ends. It keeps the session log as `.harness/last-session-Player.log`.
- **Crashes:** a hidden watchdog process restores the files when the game exits, even after a crash or a kill. If the watchdog dies too, the next `archo` command restores them, or you can run `archo restore`.
- **Mods folder:** `archo` links the harness mod, and any mod under test that isn't already installed, into `<RimWorld>/Mods` as `archo-<packageId>` directory junctions. It removes them when the session ends.
- **Another session or game:** `archo` refuses to start while another archo session or any RimWorld is running, and says which. Pass `--wait` to wait for it instead.

## Scenarios

A scenario is a C# class deriving from `HarnessScenario`, in a scenarios project. `Run()` executes on the game's main thread with full access to the game and every loaded mod, and `await` waits without freezing the game:

```csharp
namespace MyMod.Scenarios;

[Mod("../mod", Build = "dotnet build Source/MyMod/MyMod.csproj", BuildIn = "..")]
public sealed class MopsTheFloor : HarnessScenario
{
    public override async Task Run()
    {
        var room = Build.Room(World.Center, width: 11, height: 9, floor: TerrainDefOf.WoodPlankFloor, door: Rot4.South);
        var cleaner = Spawn.Colonist("Cleaner", inside: room, work: [WorkTypeDefOf.Cleaning]);
        cleaner.Equip(MyModDefOf.MyMod_Mop, stuff: ThingDefOf.WoodLog);
        Colonists.RemoveAllExcept(cleaner);

        room.Scatter(ThingDefOf.Filth_Blood, 12);
        var before = room.Filth().Count;
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth().Count < before);
        Check(cleaner.CurrentJob() == JobDefOf.Clean);
        await Shot("after");
    }
}
```

The game is paused before `Run()` starts, so the quick-test colonists stand still until your scenario sets a speed. Remove them with `Colonists.RemoveAllExcept` before you unpause, or they may walk filth into your rooms.

### Defs

Scenarios pass defs as typed objects, never as defName strings. Use RimWorld's DefOf classes (`ThingDefOf`, `TerrainDefOf`, `JobDefOf`, `WorkTypeDefOf`, `TraitDefOf`, `ThoughtDefOf`, `PawnKindDefOf` and so on) and your mod's own. For a def that no DefOf class covers, declare your own `[DefOf]` class in the scenarios project:

```csharp
[DefOf]
public static class CleaningDefOf
{
    public static ThingDef Filth_Sand;

    [DefAlias("Filth_SpentAcid")]
    public static ThingDef SpentAcid;
}
```

RimWorld binds DefOf fields only at startup, so the harness binds the `[DefOf]` classes in a scenarios assembly when it loads it. Each field takes the def named after it, or the name in `[DefAlias]`. If a def doesn't exist, the run fails before `Run()` starts and names the field. Mark a field from a DLC or another mod with `[MayRequire("packageId")]` to leave it null when that mod isn't active. `scenarios/CleaningDefOf.cs` is a working example.

`archo run FILE.cs` reads the class's attributes, builds the listed mods, then builds the scenarios project that holds the file. A build error is reported before the game starts. A runtime error is reported with the scenario file and line it came from.

### Attributes

Attribute arguments must be literals. Paths are relative to the scenario file.

| Attribute | Does |
| --- | --- |
| `[Mod("DIR")]` | Loads a mod under test; DIR holds `About/About.xml`. Repeat it for several mods. |
| `[Mod("DIR", Build = "COMMAND", BuildIn = "DIR")]` | Also runs COMMAND before launch, in BuildIn (default: the mod folder). |
| `[Dlc("biotech", "ideology")]` | Activates DLCs. |
| `[Window(1600, 900)]` | Sets the window size. |

### Scenarios project

`build/ArchotechScenarios.props` turns a .csproj into a scenarios project. It finds RimWorld, targets .NET 10, references the game, Harmony and the harness, and imports `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `Verse`, `Verse.AI`, `RimWorld`, `ArchotechHarness.Scripting` and `static ArchotechHarness.Scripting.Script`. This repo's own project is `scenarios/Scenarios.csproj`.

A mod repo keeps its own scenarios next to the mod, for example `MyMod/harness/Scenarios.csproj`. It finds the harness through the `ARCHO_HOME` environment variable, set to your harness clone, and falls back to a sibling `ArchotechHarness` folder:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ArchoHome Condition="'$(ArchoHome)' == ''">$(ARCHO_HOME)</ArchoHome>
    <ArchoHome Condition="'$(ArchoHome)' == '' And Exists('..\..\ArchotechHarness')">..\..\ArchotechHarness</ArchoHome>
  </PropertyGroup>
  <Import Project="$(ArchoHome)\build\ArchotechScenarios.props" Condition="Exists('$(ArchoHome)\build\ArchotechScenarios.props')" />

  <ItemGroup>
    <Reference Include="MyMod" HintPath="..\mod\1.7\Assemblies\MyMod.dll" Private="false" />
  </ItemGroup>

  <Target Name="RequireArchotechHarness" BeforeTargets="ResolveAssemblyReferences" Condition="!Exists('$(ArchoHome)\build\ArchotechScenarios.props')">
    <Error Text="Archotech Harness not found. Clone https://github.com/AW-Koi/archotech-harness and set ARCHO_HOME to it." />
  </Target>
</Project>
```

- The `Reference` gives scenarios typed access to the mod, such as its DefOf classes.
- Scenarios in that project use `[Mod("../mod")]`. Run one with `<harness>/archo run MyMod/harness/MyScenario.cs`, or all of them with `<harness>/archo test MyMod/harness`.
- The harness mod must be built once (`./archo --help` builds it), because the props reference `mod/1.7/Assemblies/ArchotechHarness.dll`.

### Editing in VS Code

Open a folder, not a single file, so C# Dev Kit loads the project. For this repo, open the repo folder; `ArchotechHarness.sln` includes `scenarios/Scenarios.csproj`. For a mod repo, add `harness/Scenarios.csproj` to the mod's .sln, or open the `harness` folder on its own. `dotnet build` of the project checks every scenario.

### Scenario API

| Member | Does |
| --- | --- |
| `World.Map`, `World.Center` | The current map and its centre cell. |
| `World.Filth(area, filthDefs)`, `World.Things(thingDef, area)`, `World.Pawn(name)` | Queries the map. Returns `Filth`, `Thing` and `Pawn` objects. |
| `Build.Room(center, width, height, floor, wall, stuff, door, roof)` | Clears the area, builds a finished walled room and removes any filth inside it. `floor` is a `TerrainDef`; `wall` (default `ThingDefOf.Wall`) and `stuff` (default `ThingDefOf.WoodLog`) are `ThingDef`s. Returns a `HarnessRoom` with `Bounds`, `Interior`, `Center`, `Filth(filthDefs)` and `Scatter(filthDef, count, seed)`, which returns the `Filth` it made. |
| `Spawn.Colonist(name, at, inside, work, traits, onlyWork, kind, incapableOf)` | Generates a colonist able to do `work` (`WorkTypeDef`s); with `onlyWork`, all other work is off. Traits are `new Trait(TraitDefOf.Industriousness, degree: 1)`. `kind` is a `PawnKindDef` (default `PawnKindDefOf.Colonist`). `incapableOf` (`WorkTags`) swaps in a childhood that disables those tags. Returns the `Pawn`. |
| `Spawn.Thing(thingDef, at, count, stuff, quality)`, `Spawn.Filth(filthDef, at, thickness)` | Places a thing and returns it, or makes filth and returns the `Filth` (null when the cell takes none). |
| `pawn.Equip(thingDef, stuff, quality)`, `pawn.SetWork(workTypeDef, priority)`, `pawn.CurrentJob()` | Pawn shortcuts. `Equip` returns the new `Thing`; `CurrentJob` returns the `JobDef`, or null when idle. |
| `pawn.Memories(thoughtDef)`, `pawn.HasThought(thoughtDef)` | The pawn's `Thought_Memory` list, optionally of one `ThoughtDef`. |
| `pawn.FloatMenuAt(cell)` | The right-click options the pawn gets at a cell, as `FloatMenuOption`s. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. `RemoveAllExcept` returns the pawns it removed. |
| `Camera.Look(cell, zoom)`, `Camera.Frame(room)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick` | Game speed and tick. A speed set here holds until changed. |
| `await Until(() => condition, timeoutSeconds)` | Waits, checking once per frame. Throws `TimeoutException` on timeout. |
| `await Ticks(n)`, `await Frames(n)` | Waits for game ticks or rendered frames. |
| `await Shot(name)` | Saves a screenshot to the run folder. |
| `Check(condition)` | Records a check; a failed check fails the run, and the scenario carries on. |
| `Require(condition)` | Records a check and stops the scenario if it fails. |
| `Note(key, value)` | Adds a value to the report. |
| `await Command(name, args)` | Runs any harness command below, with JSON arguments. Scenarios rarely need it. |

The report lists `passed`, `checks` (with the source text of each condition), `notes`, `shots`, `gameErrors` logged during the run, and on failure the `error` with the scenario lines in `at`.

## Running many scenarios

`archo test` finds every scenario class under the paths you give it (scenario files, folders or scenarios projects, default the current folder) and runs them:

- It boots one game per mod and DLC set. Scenarios that share a set share the game.
- Before each scenario after the first, it replaces the game with a fresh quick-test colony (`newGame`), so pawns, maps, the storyteller and game speed don't carry over. Static state inside your own mod does carry over.
- It prints one JSON line per scenario and a summary line, and writes `run.json` and `report.html` (checks, notes, screenshots, game errors and timing) to `.harness/test-runs/<timestamp>/`, or to `--out`.
- The exit code is 0 only when every scenario passed.

| Option | Does |
| --- | --- |
| `--filter GLOB` | Runs only scenarios whose class name, or full name, matches. `*` and `?` work; repeat it to match any of several. |
| `--retries N` | Runs a failed scenario up to N more times, each in a fresh colony. |
| `--wait` | Waits for another session to end instead of refusing to start. |
| `--out DIR` | Writes the reports there. |

## Commands

| Command | Does |
| --- | --- |
| `archo launch [--dlc NAME]... [--mod DIR]... [--window WxH] [--port N]` | Starts a session and returns once the map is loaded. |
| `archo run FILE.cs [--keep-open] [--attach] [--wait] [--out DIR]` | Runs one scenario class in its own session. `--attach` uses the running session instead and leaves it open. |
| `archo test [PATH]... [--filter GLOB]... [--retries N] [--wait] [--out DIR]` | Runs many scenarios; see above. |
| `archo send COMMAND ['{json args}']` | Sends one harness command and prints the response. |
| `archo capture PATH.png` | Captures the game window from outside the game. |
| `archo status` | Shows whether a session is running. |
| `archo log [--count N] [--problems]` | Prints the tail of the session log. |
| `archo stop` | Quits the game and restores your files. |
| `archo restore` | Restores your files after an unfinished session. |

DLC names are `royalty`, `ideology`, `biotech`, `anomaly` and `odyssey`. A mod directory is the folder that holds `About/About.xml`.

## Harness commands

Send them with `archo send`. A scenario can call one with `await Command(name, args)`, but the typed scenario API covers the same ground. Cells are `[x, z]` or `"center"`. Rectangles are `[x, z, width, height]`. The quick-test map is 250 by 250 cells.

| Command | Arguments | Result |
| --- | --- | --- |
| `ping` | none | `pid`. Answers even while the game loads. |
| `status` | none | Program state, `mapLoaded`, `tick`, `speed`, `paused`, map size and colonists. |
| `spawnPawn` | `cell`, `name`, `kind`, `traits` (`"Def"` or `"Def:degree"`), `skills` (`{"Cooking": 8}`), `work` (`{"Cleaning": 1}`), `onlyWork`, `incapableOf` (work tags such as `["Violent"]`) | The colonist's `id`, `name`, `cell`, `incapableOf` and `traits`. It retries until the colonist can do every listed work type. `incapableOf` swaps in one vanilla childhood that disables all those tags. |
| `spawnThing` | `def`, `cell`, `stuff`, `count`, `quality` | The thing's `id`, `cell` and `count`. |
| `equip` | `pawn`, `def`, `stuff`, `quality` | `slot`: a weapon is equipped, apparel is worn, anything else goes to the inventory. |
| `buildRoom` | `rect` (walls included), `wall`, `stuff`, `floor`, `door`, `doorDef`, `roof`, `home` | `rect`, `interior` and `center`. It clears the area first and adds the room to the home area. |
| `spawnFilth` | `def`, `cells`, or `rect` with `count` and `seed`; `thickness` | `spawned`. |
| `setWork` | `pawn`, `priorities`, `onlyWork` | The pawn's priorities above 0. |
| `removeColonists` | `keep` (names) | `removed`. |
| `speed` | `speed`: `paused`, `normal`, `fast`, `superfast`, `ultrafast` | `speed` and `tick`. |
| `camera` | `cell` or `pawn`; `zoom` (`close`, `middle`, `far`, or 11 to 60); `select` | `cell` and `rootSize`. |
| `wait` | `ticks`, `untilTick`, or `probe` with `args`, `path`, `op`, `value`; `timeoutSeconds` (default 120) | `tick`, `seconds` and the final `value`. |
| `screenshot` | `path` | `path` and `bytes`. The game's own capture includes the UI. |
| `probe` | `name`, `args` | The probe's result. |
| `listProbes` | none | All probe and command names. |
| `log` | `minLevel` (`message`, `warning`, `error`), `count`, `contains` | `errors`, `warnings` and the matching `entries`. |
| `newGame` | `mapSize` (default 250), `planetCoverage` (default 0.05), `seed` | Replaces the game with a fresh quick-test colony. `seconds`, `seed` and `mapSize`. |
| `quit` | none | Closes the game without saving. |
| `runScenario` | `assembly`, `entryType`, `outputDirectory` | Loads a built scenarios assembly, runs the class and replies with its report. `archo` sends this for you. |

`op` is one of `eq`, `ne`, `lt`, `le`, `gt` or `ge`. Without an `op`, a condition holds when the value is `true` or a non-zero number. Game time passes only while the game runs, so set a speed before a `wait`.

### Built-in probes

- `filthCount`: `rect` and `defs` are optional. Returns `count` and `byDef`.
- `thingCount`: needs `def`, and `rect` is optional. Returns `count` and `stackTotal`.
- `pawn`: needs `pawn`. Returns cell, current job, job report, weapon, apparel, drafted and downed state, and work priorities.
- `thoughts`: needs `pawn`, and `def` is optional. Returns `has` and `memories`, each with `def`, `mood` and `otherPawn`.
- `floatMenu`: needs `pawn` and `cell`, and `labelContains` is optional. Returns `enabledCount`, `disabledCount` and the right-click `options` with their labels.
- `mapLoaded` and `tick`.

### Adding probes from your mod

Add a class named `ArchotechProbes` to your mod. You don't need a reference to the harness. Each public static method that takes no argument, or one JSON string, registers as `<AssemblyName>.<MethodName>`. It runs on the main thread, and its return value becomes the JSON result:

```csharp
public static class ArchotechProbes
{
    public static int MopsOnMap() =>
        Find.CurrentMap.listerThings.ThingsOfDef(MyModDefOf.MyMod_Mop).Count;
}
```

Call it with `archo send probe '{"name":"MyMod.MopsOnMap"}'`. A mod that references `ArchotechHarness.dll` can call `ArchotechHarness.HarnessProbes.Register(name, json => resultJson)` instead.

## Game launch details

`archo` starts `RimWorld.exe` with `-quicktest -savedatafolder=<profile> -WindowSize=W,H -harnessPort=N -NoCrashDialog -DisableLogStackTraces`. The quick-test start uses the Crashlanded scenario with Cassandra on Rough, on a random 250 by 250 map. The written Prefs.xml enables dev mode, opens a window instead of fullscreen, keeps the game running unfocused and mutes the sound.

The watchdog titles the game window "Archotech Harness" and gives it `art/ArchotechHarness.ico`, in the title bar and on the taskbar. It reapplies both if the game resets them. It touches only the game it launched.

## Building and testing

```sh
dotnet build ArchotechHarness.sln
dotnet test ArchotechHarness.sln
```

The mod, its tests and the scenarios reference the game's `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.dll` from the RimWorld install that `build/FindRimWorld.props` finds. This repo contains none of the game's files.

## Icons

`art/make_icon.py` draws both icons and needs Python with Pillow:

- `python art/make_icon.py png mod/About/ModIcon.png 128` draws the mod list icon.
- `python art/make_icon.py ico art/ArchotechHarness.ico` draws the window and taskbar icon, rendering each size from 16 to 256 px separately.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
