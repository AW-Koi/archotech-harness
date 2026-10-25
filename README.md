# Archotech Harness

Start RimWorld 1.7 from a script, set up a colony, drive the game, read results back as JSON and take screenshots. It's built for AI agents and for repeatable in-game checks of your own mods.

The harness has three parts:

- `archo`, a command-line tool that prepares a dev profile, launches the game straight into a quick-test map, sends commands and restores your files afterwards.
- A dev-only mod (`TerraIncognita.ArchotechHarness`) that listens on `127.0.0.1` and runs commands on the game's main thread.
- Scenarios: C# classes in a scenarios project that `archo run` builds and runs inside the game.

## Requirements

- Windows, the .NET 10 SDK, and RimWorld 1.7 (the Waylith build).
- The Harmony mod (`brrainz.harmony`) with 1.7 support, from the Steam workshop or the Mods folder.
- Steam running, if Harmony comes from the workshop.

If RimWorld isn't installed at `D:\Program Files (x86)\Steam\steamapps\common\RimWorld`, set `RIMWORLD_DIR` to the install folder before you build or run.

## Quick start

From Git Bash:

```sh
./archo run scenarios/JanitorBasics.cs
```

From PowerShell:

```powershell
.\archo.ps1 run scenarios\JanitorBasics.cs
```

The first call builds the CLI and the mod. Set `ARCHO_REBUILD=1` to rebuild after you pull changes. A scenario run prints one JSON report line. Screenshots and the report go to `.harness/runs/<scenario>-<timestamp>/`, or to the folder you pass with `--out`.

## What happens to your game files

- **Config:** the game runs with `-savedatafolder=<repo>/.harness/profile`, so it reads and writes a separate ModsConfig.xml, Prefs.xml and Saves folder. Your own config and saves stay as they are.
- **Logs:** RimWorld always writes `Player.log` to your real data folder. `archo` backs up `ModsConfig.xml`, `Prefs.xml`, `Player.log` and `Player-prev.log` before launch, then restores them and checks their SHA-256 hashes when the session ends. It keeps the session log as `.harness/last-session-Player.log`.
- **Crashes:** a hidden watchdog process restores the files when the game exits, even after a crash or a kill. If the watchdog dies too, the next `archo` command restores them, or you can run `archo restore`.
- **Mods folder:** `archo` links the harness mod, and any mod under test that isn't already installed, into `<RimWorld>/Mods` as `archo-<packageId>` directory junctions. It removes them when the session ends.
- **A game you're playing:** `archo` refuses to start if RimWorld is already running.

## Scenarios

A scenario is a C# class deriving from `HarnessScenario`, in a scenarios project. `Run()` executes on the game's main thread with full access to the game and every loaded mod, and `await` waits without freezing the game:

```csharp
namespace ArchotechHarness.Scenarios;

[Mod("../../JanitorsCloset/mod", Build = "dotnet build Source/JanitorsCloset/JanitorsCloset.csproj", BuildIn = "../../JanitorsCloset")]
public sealed class JanitorBasics : HarnessScenario
{
    public override async Task Run()
    {
        var room = Build.Room(World.Center, width: 11, height: 9, floor: "WoodPlankFloor", door: Rot4.South);
        var mopper = Spawn.Colonist("Mopper", inside: room, work: [WorkTypeDefOf.Cleaning]);
        mopper.Equip("Janitor_Mop");
        Colonists.RemoveAllExcept(mopper);

        room.Scatter("Filth_Blood", 12);
        var before = room.Filth().Count;
        await Shot("before");

        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth().Count < before);
        Check(mopper.CurrentJob() == "Clean");
        await Shot("after");
    }
}
```

Run it by its file:

```sh
./archo run scenarios/JanitorBasics.cs
```

`archo run` reads the class's attributes, builds the listed mods, then builds the scenarios project that holds the file. A build error is reported before the game starts. A runtime error is reported with the scenario file and line it came from.

### Attributes

Attribute arguments must be literals. Paths are relative to the scenario file.

| Attribute | Does |
| --- | --- |
| `[Mod("DIR")]` | Loads a mod under test; DIR holds `About/About.xml`. Repeat it for several mods. |
| `[Mod("DIR", Build = "COMMAND", BuildIn = "DIR")]` | Also runs COMMAND before launch, in BuildIn (default: the mod folder). |
| `[Dlc("biotech", "ideology")]` | Activates DLCs. |
| `[Window(1600, 900)]` | Sets the window size. |

### Scenarios project

`build/ArchotechScenarios.props` turns a .csproj into a scenarios project. It targets .NET 10, references the game, Harmony and the harness, and imports `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `Verse`, `Verse.AI`, `RimWorld`, `ArchotechHarness.Scripting` and `static ArchotechHarness.Scripting.Script`. This repo's own project is `scenarios/Scenarios.csproj`.

A mod repo keeps its own scenarios next to the mod, for example `MyMod/harness/Scenarios.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ArchoHome Condition="'$(ArchoHome)' == ''">..\..\ArchotechHarness</ArchoHome>
  </PropertyGroup>
  <Import Project="$(ArchoHome)\build\ArchotechScenarios.props" />

  <ItemGroup>
    <Reference Include="MyMod" HintPath="..\mod\1.7\Assemblies\MyMod.dll" Private="false" />
  </ItemGroup>
</Project>
```

- Set `ArchoHome` to the harness clone, as a relative path or through the `ArchoHome` environment variable.
- The `Reference` gives scenarios typed access to the mod, such as its DefOf classes.
- Scenarios in that project use `[Mod("../mod")]`, and run with `<harness>/archo run MyMod/harness/MyScenario.cs`.
- The harness mod must be built once (`./archo --help` builds it), because the props reference `mod/1.7/Assemblies/ArchotechHarness.dll`.

### Editing in VS Code

Open a folder, not a single file, so C# Dev Kit loads the project. For this repo, open `D:/Modding/RimWorld/ArchotechHarness`; `ArchotechHarness.sln` includes `scenarios/Scenarios.csproj`. For a mod repo, add `harness/Scenarios.csproj` to the mod's .sln, or open the `harness` folder on its own. `dotnet build` of the project checks every scenario.

### Scenario API

| Member | Does |
| --- | --- |
| `World.Map`, `World.Center` | The current map and its centre cell. |
| `World.Filth(area, defs)`, `World.Things(def, area)`, `World.Pawn(name)` | Queries the map. |
| `Build.Room(center, width, height, floor, wall, stuff, door, roof)` | Clears the area and builds a finished walled room. Returns a `HarnessRoom` with `Bounds`, `Interior`, `Center`, `Filth(defs)` and `Scatter(def, count, seed)`. |
| `Spawn.Colonist(name, at, inside, work, traits, onlyWork, kind, incapableOf)` | Generates a colonist able to do `work` (WorkTypeDefs); with `onlyWork`, all other work is off. Traits are `"Def"` or `"Def:degree"`. `incapableOf` (WorkTags) swaps in a childhood that disables those tags. |
| `Spawn.Thing(def, at, count, stuff)`, `Spawn.Filth(def, at)` | Places things. |
| `pawn.Equip(def, stuff, quality)`, `pawn.SetWork(workType, priority)`, `pawn.CurrentJob()` | Pawn shortcuts. |
| `pawn.Memories(thoughtDef)`, `pawn.HasThought(thoughtDef)` | The pawn's memory thoughts. |
| `pawn.FloatMenuAt(cell)` | The right-click options the pawn gets at a cell, as `FloatMenuOption`s. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. |
| `Camera.Look(cell, zoom)`, `Camera.Frame(room)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick` | Game speed and tick. A speed set here holds until changed. |
| `await Until(() => condition, timeoutSeconds)` | Waits, checking once per frame. Throws `TimeoutException` on timeout. |
| `await Ticks(n)`, `await Frames(n)` | Waits for game ticks or rendered frames. |
| `await Shot(name)` | Saves a screenshot to the run folder. |
| `Check(condition)` | Records a check; a failed check fails the run, and the scenario carries on. |
| `Require(condition)` | Records a check and stops the scenario if it fails. |
| `Note(key, value)` | Adds a value to the report. |
| `await Command(name, args)` | Runs any harness command below. |

The report lists `passed`, `checks` (with the source text of each condition), `notes`, `shots`, `gameErrors` logged during the run, and on failure the `error` with the scenario lines in `at`.

## Commands

| Command | Does |
| --- | --- |
| `archo launch [--dlc NAME]... [--mod DIR]... [--window WxH] [--port N]` | Starts a session and returns once the map is loaded. |
| `archo run SCENARIO [--keep-open] [--out DIR]` | Runs a scenario class (`.cs`), starting a session if none is running. |
| `archo send COMMAND ['{json args}']` | Sends one harness command and prints the response. |
| `archo capture PATH.png` | Captures the game window from outside the game. |
| `archo status` | Shows whether a session is running. |
| `archo log [--count N] [--problems]` | Prints the tail of the session log. |
| `archo stop` | Quits the game and restores your files. |
| `archo restore` | Restores your files after an unfinished session. |

DLC names are `royalty`, `ideology`, `biotech`, `anomaly` and `odyssey`. A mod directory is the folder that holds `About/About.xml`.

## Harness commands

Send them with `archo send`, or call them from a scenario with `await Command(name, args)`. Cells are `[x, z]` or `"center"`. Rectangles are `[x, z, width, height]`. The quick-test map is 250 by 250 cells.

| Command | Arguments | Result |
| --- | --- | --- |
| `ping` | none | `pid`. Answers even while the game loads. |
| `status` | none | Program state, `mapLoaded`, `tick`, `speed`, `paused`, map size and colonists. |
| `spawnPawn` | `cell`, `name`, `kind`, `traits` (`"Def"` or `"Def:degree"`), `skills` (`{"Cooking": 8}`), `work` (`{"Cleaning": 1}`), `onlyWork`, `incapableOf` (work tags such as `["Violent"]`) | The colonist's `id`, `name`, `cell`, `incapableOf` and `traits`. It retries until the colonist can do every listed work type. `incapableOf` swaps in a vanilla childhood that disables those tags. |
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
| `quit` | none | Closes the game without saving. |
| `runScenario` | `assembly`, `entryType`, `outputDirectory` | Loads a built scenarios assembly, runs the class and replies with its report. `archo run` sends this for you. |

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
        Find.CurrentMap.listerThings.ThingsOfDef(ThingDef.Named("Janitor_Mop")).Count;
}
```

Call it with `archo send probe '{"name":"JanitorsCloset.MopsOnMap"}'`. A mod that references `ArchotechHarness.dll` can call `ArchotechHarness.HarnessProbes.Register(name, json => resultJson)` instead.

## Game launch details

`archo` starts `RimWorld.exe` with `-quicktest -savedatafolder=<profile> -WindowSize=W,H -harnessPort=N -NoCrashDialog -DisableLogStackTraces`. The quick-test start uses the Crashlanded scenario with Cassandra on Rough, on a random 250 by 250 map. The written Prefs.xml enables dev mode, opens a window instead of fullscreen, keeps the game running unfocused and mutes the sound.

The watchdog titles the game window "Archotech Harness" and gives it `art/ArchotechHarness.ico`, in the title bar and on the taskbar. It reapplies both if the game resets them. It touches only the game it launched.

## Building and testing

```sh
dotnet build ArchotechHarness.sln
dotnet test Source/ArchotechHarness.Cli.Tests
```

The mod references the game's `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.dll` from `RIMWORLD_DIR`. This repo contains none of the game's files.

## Icons

`art/make_icon.py` draws both icons and needs Python with Pillow:

- `python art/make_icon.py png mod/About/ModIcon.png 128` draws the mod list icon.
- `python art/make_icon.py ico art/ArchotechHarness.ico` draws the window and taskbar icon, rendering each size from 16 to 256 px separately.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
