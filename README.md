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

`Build.Room()`, `Spawn.Colonist()` and `Spawn.Thing(def)` return builders. Chain the options you need, then finish with `.Build()` or `.Spawn()`. Nothing happens until that call, and a builder you never finish fails the run.

### Waiting and checking

- `await Until(() => condition)` waits for a condition without freezing the game. It fails the run when the timeout passes.
- `await Ticks(n)` waits for game time. The game starts paused, so set `Clock.Speed` first.
- `await Reload()` saves the game, loads it back and waits until the map is playable. Look up pawns and things again afterwards, because the old references go stale.
- `Check(condition)` records a check and carries on. `Require(condition)` stops the scenario when it fails. The report shows the source text of each check.

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

Attributes on a class add to the project's mods for that scenario. Their paths are relative to the scenario file. A mod declared twice loads once.

| Attribute | Does |
| --- | --- |
| `[Mod("../mod")]` | Loads the mod in that folder. Repeat it for several mods. |
| `[Mod("../mod", Build = "dotnet build", BuildIn = "..")]` | Also builds the mod before the run. |
| `[RequiresMod("ferny.ProgressionEducation")]` | Loads an installed mod by package id, with its dependencies. Repeat it for several mods. See [Compatibility scenarios](#compatibility-scenarios). |
| `[Dlc("biotech")]` | Activates DLCs: `royalty`, `ideology`, `biotech`, `anomaly`, `odyssey`. |

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
| `Build.Room()` | Builds a walled room. Requires `.Size(w, h)`; takes `.CenteredOn(cell)`, `.WithFloor(def)`, `.WithWalls(def)`, `.MadeOf(stuff)`, `.WithDoor(rot)` and `.Roofed()`. Finish with `.Build()`. |
| `Spawn.Colonist()` | Spawns a colonist. Takes `.WithName`, `.At(cell)` or `.Inside(room)`, `.Doing(workTypes)`, `.KeepingOtherWork()`, `.IncapableOf(tags)`, `.WithTrait`, `.WithSkill` and `.OfKind`. Finish with `.Spawn()`. |
| `Spawn.Thing(def)` | Makes a thing. Takes `.At(cell)` or `.GivenTo(pawn)`, `.MadeOf`, `.OfQuality` and `.Stack`. Finish with `.Spawn()`. |
| `Spawn.Filth(def, cell)` | Places filth. |
| `room.Scatter(def, count)`, `room.Filth(defs)` | Spreads filth over a room, or lists the filth in it. |
| `World.Map`, `World.Center`, `World.Filth`, `World.Things`, `World.Pawn` | The current map and queries over it. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. |
| `pawn.CurrentJob()`, `pawn.SetWork`, `pawn.HasThought`, `pawn.Memories`, `pawn.FloatMenuAt(cell)` | Pawn shortcuts. |
| `Camera.Look`, `Camera.Frame(room)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick` | Game speed and tick. |
| `Until`, `Ticks`, `Frames` | Waits. |
| `Check`, `Require`, `Note`, `Shot` | Records checks, values and screenshots. |

### Isolation and your files

Each game runs with its own save-data folder under `.harness/`, so it never reads or writes your saves, mod list or settings. RimWorld still writes `Player.log` to your data folder, so `archo` backs up your config and logs before the first game. It restores them, checking every hash, when the last game closes. A watchdog does the same if the game crashes or is killed, and `archo restore` covers anything that slips through. `archo` won't start while you're playing RimWorld, or while another `archo` on this machine has a game open.

## Further reading

- [docs/harness-commands.md](docs/harness-commands.md): the JSON commands behind `archo send`.
- [CONTRIBUTING.md](CONTRIBUTING.md): how the harness works, and how to build and test it.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
