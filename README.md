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

The first run builds `archo` and the harness mod. The game opens, the scenario runs and the game closes. `archo` prints a JSON report and saves it, with the screenshots, in `.harness/runs/<scenario>-<time>/`.

Run every scenario in a folder with `./archo test scenarios`. That writes `report.html` to `.harness/test-runs/<time>/`, with each scenario's checks, notes, screenshots and a timeline of its steps.

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

Attributes on the class say what the game loads. Paths are relative to the scenario file.

| Attribute | Does |
| --- | --- |
| `[Mod("../mod")]` | Loads the mod in that folder. Repeat it for several mods. |
| `[Mod("../mod", Build = "dotnet build", BuildIn = "..")]` | Also builds the mod before the run. |
| `[Dlc("biotech")]` | Activates DLCs: `royalty`, `ideology`, `biotech`, `anomaly`, `odyssey`. |

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
  </ItemGroup>

  <Target Name="RequireArchotechHarness" BeforeTargets="ResolveAssemblyReferences" Condition="!Exists('$(ArchoHome)\build\ArchotechScenarios.props')">
    <Error Text="Archotech Harness not found. Clone https://github.com/AW-Koi/archotech-harness and set ARCHO_HOME to it." />
  </Target>
</Project>
```

1. Set the `ARCHO_HOME` environment variable to your harness clone. A clone in a sibling `ArchotechHarness` folder also works.
2. Run `./archo --help` in the harness once, so the harness mod is built.
3. Change the `Reference` to your mod's assembly, so scenarios can use its types and DefOf classes.
4. Put `[Mod("../mod")]` on your scenarios, then run them with `<harness>/archo test MyMod/harness`.

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
| `archo test --video` | Records a video of each scenario into the report. It needs ffmpeg, on the PATH, installed with WinGet, or named by the `FFMPEG` environment variable. `--video-fps N` sets the frame rate (default 5). `run` takes it too. |
| `archo launch` | Starts a game and leaves it running for `send`. `--mod DIR` and `--dlc NAME` choose what it loads. |
| `archo send COMMAND ['{json}']` | Sends one command to the running game, such as `status`. |
| `archo status` | Lists your running games. |
| `archo stop` | Closes your games and restores your files. |
| `archo restore` | Restores your files after an interrupted run. |

Add `--wait` to `run` or `test` to wait for another game to close instead of stopping with an error. `archo test` exits with code 0 only when every scenario passed.

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
