# RimWorld dev harness

Start RimWorld 1.7 from a script, set up a colony, drive the game, read results back as JSON and take screenshots. It's built for AI agents and for repeatable in-game checks of your own mods.

The harness has three parts:

- `rwh`, a command-line tool that prepares a dev profile, launches the game straight into a quick-test map, sends commands and restores your files afterwards.
- A dev-only mod (`TerraIncognita.RimWorldDevHarness`) that listens on `127.0.0.1` and runs commands on the game's main thread.
- Scenarios: C# scripts (`.csx`) that `rwh run` compiles and runs inside the game. JSON step lists also work.

## Requirements

- Windows, the .NET 10 SDK, and RimWorld 1.7 (the Waylith build).
- The Harmony mod (`brrainz.harmony`) with 1.7 support, from the Steam workshop or the Mods folder.
- Steam running, if Harmony comes from the workshop.

If RimWorld isn't installed at `D:\Program Files (x86)\Steam\steamapps\common\RimWorld`, set `RIMWORLD_DIR` to the install folder before you build or run.

## Quick start

From Git Bash:

```sh
./rwh run scenarios/janitor-basics.csx
```

From PowerShell:

```powershell
.\rwh.ps1 run scenarios\janitor-basics.csx
```

The first call builds the CLI and the mod. Set `RWH_REBUILD=1` to rebuild after you pull changes. A script run prints one JSON report line. Screenshots and the report go to `.harness/runs/<scenario>-<timestamp>/`, or to the folder you pass with `--out`.

## What happens to your game files

- **Config:** the game runs with `-savedatafolder=<repo>/.harness/profile`, so it reads and writes a separate ModsConfig.xml, Prefs.xml and Saves folder. Your own config and saves stay as they are.
- **Logs:** RimWorld always writes `Player.log` to your real data folder. `rwh` backs up `ModsConfig.xml`, `Prefs.xml`, `Player.log` and `Player-prev.log` before launch, then restores them and checks their SHA-256 hashes when the session ends. It keeps the session log as `.harness/last-session-Player.log`.
- **Crashes:** a hidden watchdog process restores the files when the game exits, even after a crash or a kill. If the watchdog dies too, the next `rwh` command restores them, or you can run `rwh restore`.
- **Mods folder:** `rwh` links the harness mod, and any mod under test that isn't already installed, into `<RimWorld>/Mods` as `rwh-<packageId>` directory junctions. It removes them when the session ends.
- **A game you're playing:** `rwh` refuses to start if RimWorld is already running.

## Scenario scripts

A scenario is a C# script. It runs on the game's main thread with full access to the game and every loaded mod, and `await` waits without freezing the game:

```csharp
#mod "../../JanitorsCloset/mod"
#build "dotnet build Source/JanitorsCloset/JanitorsCloset.csproj" in "../../JanitorsCloset"
using JanitorsCloset.Defs;

var room = Build.Room(World.Center, width: 11, height: 9, floor: "WoodPlankFloor", door: Rot4.South);
var mopper = Spawn.Colonist("Mopper", inside: room, work: [WorkTypeDefOf.Cleaning]);
mopper.Equip(JanitorDefOf.Janitor_Mop.defName);
Colonists.RemoveAllExcept(mopper);

room.Scatter("Filth_Blood", 12);
var before = room.Filth().Count;
await Shot("before");

Clock.Speed = TimeSpeed.Superfast;
await Until(() => room.Filth().Count < before);
Check(mopper.CurrentJob() == "Clean");
await Shot("after");
```

`rwh run` builds the listed mods and compiles the script before it launches the game. A compile error is reported as `file(line,column)` and the game never starts. A runtime error is reported with the script line it came from.

### Directives

Each directive goes on its own line at the top of the script. Paths are relative to the script.

| Directive | Does |
| --- | --- |
| `#mod "DIR"` | Loads a mod under test; DIR holds `About/About.xml`. Its assemblies become references. |
| `#build "COMMAND" in "DIR"` | Runs COMMAND in DIR before launch, for the `#mod` above it. |
| `#dlc biotech, ideology` | Activates DLCs. |
| `#window 1600x900` | Sets the window size. |
| `#load "helpers.cs"` | Compiles a C# file with the script, for shared classes and setup. |

The script imports `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `Verse`, `Verse.AI`, `RimWorld` and `DevHarness.Scripting`. Add more `using` lines at the top.

### Script API

| Member | Does |
| --- | --- |
| `World.Map`, `World.Center` | The current map and its centre cell. |
| `World.Filth(area, defs)`, `World.Things(def, area)`, `World.Pawn(name)` | Queries the map. |
| `Build.Room(center, width, height, floor, wall, stuff, door, roof)` | Clears the area and builds a finished walled room. Returns a `HarnessRoom` with `Bounds`, `Interior`, `Center`, `Filth(defs)` and `Scatter(def, count, seed)`. |
| `Spawn.Colonist(name, at, inside, work, traits, onlyWork)` | Generates a colonist able to do `work` (WorkTypeDefs); with `onlyWork`, all other work is off. Traits are `"Def"` or `"Def:degree"`. |
| `Spawn.Thing(def, at, count, stuff)`, `Spawn.Filth(def, at)` | Places things. |
| `pawn.Equip(def, stuff, quality)`, `pawn.SetWork(workType, priority)`, `pawn.CurrentJob()` | Pawn shortcuts. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. |
| `Camera.Look(cell, zoom)`, `Camera.Frame(room)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick` | Game speed and tick. A speed set here holds until changed. |
| `await Until(() => condition, timeoutSeconds)` | Waits, checking once per frame. Throws `TimeoutException` on timeout. |
| `await Ticks(n)`, `await Frames(n)` | Waits for game ticks or rendered frames. |
| `await Shot(name)` | Saves a screenshot to the run folder. |
| `Check(condition)` | Records a check; a failed check fails the run, and the script carries on. |
| `Require(condition)` | Records a check and stops the script if it fails. |
| `Note(key, value)` | Adds a value to the report. |
| `await Command(name, args)` | Runs any harness command below. |

The report lists `passed`, `checks` (with the source text of each condition), `notes`, `shots`, `gameErrors` logged during the run, and on failure the `error` with the script lines in `at`.

## Commands

| Command | Does |
| --- | --- |
| `rwh launch [--dlc NAME]... [--mod DIR]... [--window WxH] [--port N]` | Starts a session and returns once the map is loaded. |
| `rwh run SCENARIO [--keep-open] [--out DIR]` | Runs a `.csx` script or a `.json` step list, starting a session if none is running. |
| `rwh send COMMAND ['{json args}']` | Sends one harness command and prints the response. |
| `rwh capture PATH.png` | Captures the game window from outside the game. |
| `rwh status` | Shows whether a session is running. |
| `rwh log [--count N] [--problems]` | Prints the tail of the session log. |
| `rwh stop` | Quits the game and restores your files. |
| `rwh restore` | Restores your files after an unfinished session. |

DLC names are `royalty`, `ideology`, `biotech`, `anomaly` and `odyssey`. A mod directory is the folder that holds `About/About.xml`.

## Harness commands

Send them with `rwh send`, or use them as scenario steps. Cells are `[x, z]` or `"center"`. Rectangles are `[x, z, width, height]`. The quick-test map is 250 by 250 cells.

| Command | Arguments | Result |
| --- | --- | --- |
| `ping` | none | `pid`. Answers even while the game loads. |
| `status` | none | Program state, `mapLoaded`, `tick`, `speed`, `paused`, map size and colonists. |
| `spawnPawn` | `cell`, `name`, `kind`, `traits` (`"Def"` or `"Def:degree"`), `skills` (`{"Cooking": 8}`), `work` (`{"Cleaning": 1}`), `onlyWork` | The colonist's `id`, `name`, `cell` and `traits`. It retries until the colonist can do every listed work type. |
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
| `runScript` | `assembly`, `entryType`, `outputDirectory` | Loads a compiled script and replies with its report when it ends. `rwh run` sends this for you. |

`op` is one of `eq`, `ne`, `lt`, `le`, `gt` or `ge`. Without an `op`, a condition holds when the value is `true` or a non-zero number. Game time passes only while the game runs, so set a speed before a `wait`.

### Built-in probes

- `filthCount`: `rect` and `defs` are optional. Returns `count` and `byDef`.
- `thingCount`: needs `def`, and `rect` is optional. Returns `count` and `stackTotal`.
- `pawn`: needs `pawn`. Returns cell, current job, job report, weapon, apparel, drafted and downed state, and work priorities.
- `mapLoaded` and `tick`.

### Adding probes from your mod

Add a class named `DevHarnessProbes` to your mod. You don't need a reference to the harness. Each public static method that takes no argument, or one JSON string, registers as `<AssemblyName>.<MethodName>`. It runs on the main thread, and its return value becomes the JSON result:

```csharp
public static class DevHarnessProbes
{
    public static int MopsOnMap() =>
        Find.CurrentMap.listerThings.ThingsOfDef(ThingDef.Named("Janitor_Mop")).Count;
}
```

Call it with `rwh send probe '{"name":"JanitorsCloset.MopsOnMap"}'`. A mod that references `DevHarness.dll` can call `DevHarness.HarnessProbes.Register(name, json => resultJson)` instead.

## JSON scenario format

JSON step lists run the harness commands in order. They suit quick, linear checks; use a script for anything with logic.

```json
{
  "name": "janitor-basics",
  "dlcs": ["biotech"],
  "mods": [
    { "dir": "../../JanitorsCloset/mod",
      "build": { "command": "dotnet build Source/JanitorsCloset/JanitorsCloset.csproj", "workingDir": "../../JanitorsCloset" } }
  ],
  "window": [1600, 900],
  "quitWhenDone": true,
  "steps": [
    { "cmd": "buildRoom", "rect": [119, 119, 11, 9], "floor": "WoodPlankFloor", "door": [124, 119], "save": "room" },
    { "cmd": "probe", "name": "filthCount", "args": { "rect": "${room.interior}" }, "save": "before" },
    { "cmd": "wait", "probe": "filthCount", "args": { "rect": "${room.interior}" }, "path": "count", "op": "lt", "value": "${before.count}" },
    { "cmd": "screenshot", "path": "after.png", "always": true }
  ]
}
```

- `dlcs`, `mods`, `window` and `quitWhenDone` apply only when `rwh run` starts the session. Paths are relative to the scenario file.
- Every key of a step other than the ones below goes to the command as an argument.
- `save` stores the step result under a name. `"${name.path}"` inserts a saved value in a later step, and keeps its JSON type when it fills the whole string. `${run.dir}` is the run folder.
- `expect` (`path`, `op`, `value`) checks the result. A mismatch fails the step.
- `optional: true` lets the scenario continue after the step fails.
- `always: true` runs the step even after an earlier step failed. Use it for final screenshots.
- A relative `path` is resolved against the run folder.
- `windowCapture` (`path`) runs in the CLI. It captures the game window with `PrintWindow`, which needs the window to be visible but not focused.

The exit code is 0 when every step that isn't optional passed.

## Game launch details

`rwh` starts `RimWorld.exe` with `-quicktest -savedatafolder=<profile> -WindowSize=W,H -harnessPort=N -NoCrashDialog -DisableLogStackTraces`. The quick-test start uses the Crashlanded scenario with Cassandra on Rough, on a random 250 by 250 map. The written Prefs.xml enables dev mode, opens a window instead of fullscreen, keeps the game running unfocused and mutes the sound.

## Building and testing

```sh
dotnet build RimWorldDevHarness.sln
dotnet test Source/DevHarness.Cli.Tests
```

The mod references the game's `Assembly-CSharp.dll`, `UnityStubs.dll` and `Waylith.dll` from `RIMWORLD_DIR`. This repo contains none of the game's files.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
