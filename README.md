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
        // A cleaner alone in a wooden room
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

        // Blood on the floor
        room.Scatter(ThingDefOf.Filth_Blood, 10);
        var before = room.Filth().Count;
        await Shot("before");

        // The cleaner gets to work
        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => room.Filth().Count < before, timeoutSeconds: 120);

        // The blood went because the cleaner cleaned it
        Check(cleaner.CurrentJob() == JobDefOf.Clean);
        await Shot("after");
    }
}
```

Save it in `scenarios/` and run it with `./archo run scenarios/CleanerCleans.cs`. Open each block of a scenario with a one-line comment that says what it stages or checks.

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

### Staging a battle

- `Spawn.Group(faction).Of(kind, count)` spawns one faction's pawns as a group with a lord. Repeat `.Of` for mixed groups. Place it with `.At(cell)` or `.FromEdge(side)`, which picks ground on that edge that reaches the colony. `.Armed(weapon)` replaces every member's weapon. A hostile group assaults the colony and any other group visits, unless you pick `.Assaulting()`, `.Visiting()` or `.Defending()`.
- `Spawn.Raid(points)` fires a raid even after `World.Calm()`. It takes `.From(faction)`, `.Strategy(def)` and `.Arriving(def)`; the game picks whatever you leave out. `.Spawn()` returns the raiders, and fails the run when the raid can't fire.
- `Spawn.Projectile(def).At(cell)` launches shells or other projectiles at a cell. A shell def fires the projectile it loads. `.FromEdge(side, distance)` sets where they come from (north, 60 cells away, without it), `.Scatter(radius)` spreads where they land and `.Count(n)` fires several.
- `Spawn.Turret(def).At(cell)` places a turret or mortar with a full barrel. `.OwnedBy(faction)` sets its owner, `.Loaded(shell)` loads it and piles spare shells beside it, and `.Manned(kind)` or `.MannedBy(pawn)` gives it a crew. An enemy crew mans it and fires at the colony.
- `Build.Line(def, from, to)` builds an L-shaped line of buildings, such as sandbags, along x then along z. Vanilla has no trench, so lay a strip of floor such as `TerrainDefOf.PackedDirt` with `Build.Floor` instead.
- `Spawn.Colonist()` takes `.Armed(weapon)`, `.Wearing(apparel)` (repeatable) and `.Drafted()`. `pawn.Draft()` drafts a pawn where it stands, and `pawn.HoldAt(cell)` drafts it and sends it to the cell, where it holds and fires at will.
- `World.Faction(def)` returns the world's first faction of that def, and generates one when the world has none.

### Hurting and healing pawns

- `pawn.Wound(injury, part, severity)` adds an injury, such as `HediffDefOf.Cut` or the `Gunshot` def, to the first intact part of that def. Injuries bleed by their own rules, so a deep torso wound bleeds out.
- `pawn.RemovePart(part)` removes a part as a tended stump; `fresh: true` leaves it bleeding.
- `pawn.AddHediff(def, part, severity)` adds any hediff, such as `HediffDefOf.WoundInfection` on a part or `HediffDefOf.Anesthetic` to keep a pawn unconscious.
- `pawn.Down()` damages a pawn until it is downed, without bleeding wounds. Kill a pawn with RimWorld's own `pawn.Kill(null)`.
- `pawn.PutInBed(bed)` puts a pawn in a bed at once, tired so it sleeps; `awake: true` leaves it awake. A downed pawn in a medical bed is a patient.
- `pawn.StartMentalState(def)` starts a mental state now, such as `MentalStateDefOf.Wander_Sad`, and returns whether it started.
- `Spawn.Thing(bed).Medical()` makes a medical bed, and `.ForPrisoners()` a prisoner bed.

### Who pawns are

- `Spawn.Colonist()` takes `.Aged(years)`, `.Xenotype(def)` and `.WithBackstory(def)`, which replaces the childhood or adulthood by the backstory's slot. A child or baby, and a xenotype, need `[Dlc("biotech")]`.
- `pawn.GrantTitle(title)` grants a royal title of the Empire, or of the faction you pass, with no rewards or letter. It needs `[Dlc("royalty")]`.
- `pawn.AssignRole(def)` gives a pawn the role of its ideoligion with that def, such as `PreceptDefOf.IdeoRole_Leader`. It needs `[Dlc("ideology")]`.
- `pawn.Relate(other, def)` adds a relation, such as `PawnRelationDefOf.Spouse`, `Fiance`, `Lover`, `ExLover` or `Parent`, where `other` is the pawn's spouse, parent and so on.
- `pawn.SetOpinionOf(other, opinion)` sets an opinion from -100 to 100 through one lasting social memory, so 20 makes a friend and -20 a rival. Later thoughts move it as they would in play.
- `Spawn.Group(Faction.OfEmpire).Of(noble).Of(guard, 2).Visiting()` brings a noble and guards as one visiting lord.

Put DLC defs in your DefOf class with `[MayRequireRoyalty]` and the like, so scenarios without that DLC still load.

### Prisoners

- `Spawn.Prisoner()` spawns a prisoner of the colony. `.From(faction)` picks where they came from (`Faction.OfPirates` without it), and `.OfKind(kind)` the pawn kind. Place them with `.At(cell)`, `.Inside(room)` or `.InBed(bed)`, which assigns the bed and puts them in it asleep. It also takes `.WithName`, `.WithTrait`, `.WithSkill`, `.Aged` and `.WithBackstory`.
- A room whose beds are all `Spawn.Thing(bed).ForPrisoners()` is a prison cell.

### Blueprints

- `Build.Room().AsBlueprint()` places blueprints for the walls, doors and floor in place of a finished room, and marks the roof to build when `.Roofed()`. Rooms built with `.Adjoining()` share a wall as finished rooms do.
- `Spawn.Thing(def).AsBlueprint()` places a blueprint of a building `.At` a cell, with `.Facing` and `.MadeOf`. It takes no starting state such as `.Fuelled()` or bills. `Build.Line(...).AsBlueprint()` does the same for a line.
- `World.Unbuilt(area)` lists the player's blueprints and construction frames still to finish, so `await FastForwardUntil(() => World.Unbuilt(house).Count == 0, 3 * GenDate.TicksPerDay)` waits for colonists to finish building.
- Colonists need materials and food to build: put stacks of at most the def's stack limit, such as 75 wood, in a stockpile.

### Ceremonies

- `Ceremony.Wedding(first, second, spot)` makes the pair fiancés if they aren't, and starts their marriage ceremony now, even in a calm world. Without a spot the game picks a marriage site, and the run fails when there is none. The pair are married when it ends. It returns the ceremony's lord.
- `Ceremony.Party(organizer)` starts a party the pawn hosts at a gathering spot, such as a table, and returns its lord.

### Furnishing presets

Presets furnish a built room in one call, on free floor only. They never use the cell inside a door, interaction cells or the space beside a bed. Each piece tries its preferred spot, then corners, wall-side cells and any free floor. A piece that still doesn't fit is left out and noted in the report, such as "placed 0 of 1 plant pots, no free floor".

- `Furnish.Bedroom(room)` puts the bed's head against the wall opposite the door, an end table either side and a dresser. It takes `.Bed(def)`, `.MadeOf(stuff)`, `.Floor(def)`, `.Lamp()`, `.Plants(plant)`, `.Art(sculpture, stuff)` and `.Owner(pawns)`.
- `Furnish.Dining(room, area)` fills the area with tables and chairs facing them. It takes `.Table(def)` (`Table2x2c` without it) and `.MadeOf(stuff)`.
- `Decorate.Room(room)` takes `.PlantPots(plant)` (a pot in each free corner), `.Lamps()` (enough standing lamps to light the room) and `.Art(sculpture, stuff)`.

Each `.Build()` returns a `FurnishedRoom` with the `Things` it placed, `Bed` and `Of(def)`. Place work tables and other fittings before `Decorate.Room`, so it keeps their interaction cells clear.

### Waiting and checking

- `await Until(() => condition)` waits for a condition without freezing the game. It fails the run when the timeout passes.
- `await Ticks(n)` waits for game time. The game starts paused, so set `Clock.Speed` first.
- `await FastForward(ticks)` runs game time as fast as the machine allows while still drawing about 60 frames a second, so whole days pass in seconds. `await FastForwardUntil(() => condition, maxTicks)` stops early when the condition holds and fails the run when `maxTicks` pass first. The time controls show Ultrafast meanwhile, and `Clock.Speed` returns to what it was.
- `await Reload()` saves the game, loads it back and waits until the map is playable. Look up pawns and things again afterwards, because the old references go stale.
- `Check(condition)` records a check and carries on. `Require(condition)` stops the scenario when it fails. The report shows the source text of each check.

### Scenario debugger

When `archo run` or `archo test` runs scenarios in a visible game, a "Scenario debugger" panel opens over the game. It lists the scenario's steps as they happen (builds, spawns, checks in green or red, notes, screenshots and finished waits), and highlights what the script waits for now. Drag it anywhere in the game window.

- **Pause** stops the script at its next `await` and pauses the game, so you can play: change the speed, select pawns and give orders.
- **Resume** carries on from there at the speed the script last set.
- **Step** runs the script up to its next `await`, then pauses it again.

Time spent paused doesn't count towards an `Until` timeout, and ticks you run while paused don't count towards `Ticks`. A scenario run still stops after 30 minutes, pauses included. Screenshots and videos leave the panel out, and the panel stays closed while a scenario records video. Pass `--no-panel` to run without it. `archo send debugger '{"action":"pause"}'` does the same from a script; the actions are `state`, `pause`, `resume` and `step`.

### Game errors

A scenario fails when the game logs an error while it runs, even if every check passes. The report lists each distinct error with its count. Errors from loading the game are reported separately, under "Game load".

Put `[AllowGameErrors]` on the class to let it pass anyway, or `[AllowGameErrors("text")]` to accept only errors that contain that text (case-sensitive). Repeat the attribute for several texts.

### Screenshots, notes and attachments

`await Shot("name")` saves a screenshot. `Note("key", value)` adds a value to the report.

`AttachFile(path)` copies a file, such as a conversation transcript your mod wrote, into the run folder and lists it in the report. `AttachText("transcript.txt", text)` writes the text there instead. The HTML report shows `.txt`, `.log`, `.md`, `.json` and `.csv` attachments inline and links any other file, such as an HTML page.

### Scale and speed

- `Spawn.Colonists(count)` spawns many colonists at once, without the retries `Spawn.Colonist()` makes for work types. `.Within(area)` spreads them over that area and `.OfKind(kind)` picks the kind.
- `await TicksPerSecond(seconds)` measures game ticks and frames per second over that much real time, notes both as `ticksPerSecond` and `framesPerSecond`, and returns ticks per second. Set `Clock.Speed` first.

### Random seed

Each run starts the game's random numbers from a seed just before `Run`, and the report records it as `seed`. Put `[Seed(1234)]` on the class to choose it, such as to repeat a run. The seed repeats the scenario's random draws only from the same starting map, and each game generates a new quick-test map, so a scenario that depends on the map can still differ between games.

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
| `[Seed(1234)]` | Starts the game's random numbers from that seed before `Run`. |

### Compatibility scenarios

A compatibility scenario runs your mod alongside another mod you have installed. Name the other mod by its package id, so the scenario works on any machine:

```csharp
[RequiresMod("ferny.ProgressionEducation")]
public sealed class EducationGatesTheSprayer : HarnessScenario
{
    public override async Task Run()
    {
        // The sprayer needs the education mod's proficiency to use
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
| `archo test --no-panel` | Runs without the scenario debugger panel. `run` takes it too. |
| `archo test --allow-load-errors` | Runs the scenarios even when a mod under test logged errors while the game loaded. `run` takes it too. See [Load errors](#load-errors). |
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

When `archo run` or `archo test` boots a game, it reads every error the game logged while it loaded and set up its first colony, such as failed patch operations, config errors and exceptions. It blames an error on a mod under test when the error starts with the mod's name or package id in brackets, names one of its defs or def files, names a file in its folder, or names a type in one of its assembly namespaces. An error from a mod under test fails the run, or every scenario in that game's group, and the failure quotes the first three. Errors from the base game or other mods show as warnings in the console and the report. Pass `--allow-load-errors` to run the scenarios anyway.

When an error stops the game's first data load, the game loads its data a second time on top of the first and logs an error for every def it adds twice. The console and the report name the mod that broke the first load and count the errors from the second, so fix the first error and the rest go with it.

`archo launch` reads the errors once its game has loaded and shows them as warnings. `archo run --attach` then checks the same errors against the scenario's mods. A failed check with `--keep-open` leaves the game running.

## Reference

### Scenario API

| Member | Does |
| --- | --- |
| `Build.Room()` | Builds a walled room. Requires `.Size(w, h)` or `.Covering(rect)`; takes `.CenteredOn(cell)`, `.WithFloor(def)`, `.WithWalls(def)`, `.MadeOf(stuff)`, `.WithDoor(rot)`, `.WithDoorAt(cell)`, `.Adjoining()`, `.Powered()`, `.Roofed()` and `.AsBlueprint()`. Finish with `.Build()`. |
| `Build.Stockpile(rect)`, `Build.GrowingZone(rect)` | Makes a zone. A stockpile takes `.Preset(preset)`; a growing zone requires `.Planting(plant)` and takes `.Grown(growth)`. Finish with `.Build()`. |
| `Build.Floor(rect, def)`, `Build.Clearing(rect)` | Lays a floor, or clears an area to open ground. |
| `Build.Path(from, to)`, `Build.InWall(def, cell, facing)`, `Build.Conduit(from, to)` | Lays an L-shaped path, mounts a building in a wall, or lays a conduit. |
| `Furnish.Bedroom(room)`, `Furnish.Dining(room, area)`, `Decorate.Room(room)` | Furnishes or decorates a room. Finish with `.Build()`. |
| `Spawn.Plant(def)`, `Spawn.Animal(kind)` | Plants a tree or flower, or adds an animal. Finish with `.Spawn()`. |
| `Spawn.Colonist()` | Spawns a colonist. Takes `.WithName`, `.At(cell)` or `.Inside(room)`, `.Doing(workTypes)`, `.KeepingOtherWork()`, `.IncapableOf(tags)`, `.WithTrait`, `.WithSkill`, `.OfKind`, `.Armed(weapon)`, `.Wearing(apparel)`, `.Drafted()`, `.Aged(years)`, `.Xenotype(def)` and `.WithBackstory(def)`. Finish with `.Spawn()`. |
| `Spawn.Thing(def)` | Makes a thing. Takes `.At(cell)` or `.GivenTo(pawn)`, `.MadeOf`, `.OfQuality`, `.Stack` and, for a building, `.Facing(rot)`, `.Fuelled()`, `.Charged()`, `.Growing(plant)`, `.WithBill(recipe)` and `.Holding(def, count)`, and for a bed `.Medical()` and `.ForPrisoners()`. `.AsBlueprint()` places a blueprint instead. Finish with `.Spawn()`. |
| `Spawn.Filth(def, cell)` | Places filth. |
| `Spawn.Prisoner()` | Spawns a prisoner of the colony. Requires one of `.At(cell)`, `.Inside(room)` or `.InBed(bed)`; takes `.From(faction)`, `.OfKind`, `.WithName`, `.WithTrait`, `.WithSkill`, `.Aged` and `.WithBackstory`. Finish with `.Spawn()`. |
| `Spawn.Group(faction)` | Spawns a group of one faction's pawns with a lord. Requires `.Of(kind, count)` and one of `.At(cell)` or `.FromEdge(side)`; takes `.Armed(weapon)`, `.Assaulting()`, `.Visiting()` and `.Defending()`. Finish with `.Spawn()`. |
| `Spawn.Raid(points)` | Fires a raid, even in a calm world. Takes `.From(faction)`, `.Strategy(def)` and `.Arriving(def)`. Finish with `.Spawn()`. |
| `Spawn.Projectile(def)` | Fires shells or projectiles. Requires `.At(cell)`; takes `.FromEdge(side, distance)`, `.Scatter(radius)` and `.Count(n)`. Finish with `.Spawn()`. |
| `Spawn.Turret(def)` | Places a turret or mortar. Requires `.At(cell)`; takes `.Facing(rot)`, `.OwnedBy(faction)`, `.Loaded(shell, spares)`, `.Manned(kind)` and `.MannedBy(pawn)`. Finish with `.Spawn()`. |
| `Build.Line(def, from, to)` | Builds an L-shaped line of buildings. Takes `.MadeOf(stuff)` and `.AsBlueprint()`. Finish with `.Build()`. |
| `pawn.Draft()`, `pawn.HoldAt(cell)` | Drafts a pawn, or drafts it and orders it to a cell. |
| `World.Faction(def)` | The first faction of a def, generated when missing. |
| `World.Unbuilt(area)` | The player's blueprints and frames still to finish. |
| `Ceremony.Wedding(first, second, spot)`, `Ceremony.Party(organizer)` | Starts a wedding or a party now. |
| `pawn.Wound`, `pawn.RemovePart`, `pawn.AddHediff`, `pawn.Down`, `pawn.PutInBed`, `pawn.StartMentalState` | Hurts, downs, beds or breaks a pawn. |
| `pawn.GrantTitle`, `pawn.AssignRole`, `pawn.Relate`, `pawn.SetOpinionOf` | Sets a pawn's title, role, relations and opinions. |
| `room.Scatter(def, count)`, `room.Filth(defs)` | Spreads filth over a room, or lists the filth in it. |
| `World.Map`, `World.Center`, `World.Filth`, `World.Things`, `World.Pawn` | The current map and queries over it. |
| `World.Calm()`, `World.RemoveHostiles()`, `World.Home(rect)` | Stops incidents and keeps clear weather, removes hostile and dormant pawns, or marks home area. |
| `Colonists.All`, `Colonists.RemoveAllExcept(pawns)` | The colonists on the map. |
| `pawn.CurrentJob()`, `pawn.SetWork`, `pawn.HasThought`, `pawn.Memories`, `pawn.FloatMenuAt(cell)` | Pawn shortcuts. |
| `Camera.Look`, `Camera.Frame(room)`, `Camera.Frame(rect)`, `Camera.Follow(pawn)` | Points the camera. |
| `Clock.Speed`, `Clock.Tick`, `Clock.SkipToHour(hour)` | Game speed and tick, and a jump to an hour of the day. |
| `Until`, `Ticks`, `Frames`, `FastForward`, `FastForwardUntil` | Waits, or runs game time as fast as possible. |
| `Check`, `Require`, `Note`, `Shot` | Records checks, values and screenshots. |
| `AttachFile(path)`, `AttachText(name, text)` | Adds a file to the run folder and the report. |
| `TicksPerSecond(seconds)` | Measures and notes ticks and frames per second. |
| `Spawn.Colonists(count)` | Spawns many colonists at once. Takes `.Within(area)` and `.OfKind(kind)`. Finish with `.Spawn()`. |

### Isolation and your files

Each game runs with its own save-data folder under `.harness/`, so it never reads or writes your saves, mod list or settings. RimWorld still writes `Player.log` to your data folder, so `archo` backs up your config and logs before the first game. It restores them, checking every hash, when the last game closes. A watchdog does the same if the game crashes or is killed, and `archo restore` covers anything that slips through. `archo` won't start while you're playing RimWorld, or while another `archo` on this machine has a game open.

## Further reading

- [docs/harness-commands.md](docs/harness-commands.md): the JSON commands behind `archo send`.
- [CONTRIBUTING.md](CONTRIBUTING.md): how the harness works, and how to build and test it.

## Licence

MIT. See [LICENSE](LICENSE). RimWorld is the property of Ludeon Studios.
