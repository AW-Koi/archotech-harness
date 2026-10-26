# Harness commands

The harness mod answers newline-delimited JSON on `127.0.0.1`. `archo send COMMAND '{json}'` sends one command to your running game; `--session ID` picks a worker game. Scenarios rarely need these, because the scenario API covers the same ground, but `await Command(name, args)` calls one from a scenario.

Cells are `[x, z]` or `"center"`. Rectangles are `[x, z, width, height]`.

| Command | Arguments | Result |
| --- | --- | --- |
| `ping` | none | `pid`. Answers even while the game loads. |
| `status` | none | Program state, `mapLoaded`, `tick`, `speed`, `paused`, map size and colonists. |
| `spawnPawn` | `cell`, `name`, `kind`, `traits` (`"Def"` or `"Def:degree"`), `skills` (`{"Cooking": 8}`), `work` (`{"Cleaning": 1}`), `onlyWork`, `incapableOf` (`["Violent"]`) | The colonist's `id`, `name`, `cell`, `incapableOf` and `traits`. |
| `spawnThing` | `def`, `cell`, `stuff`, `count`, `quality` | The thing's `id`, `cell` and `count`. |
| `equip` | `pawn`, `def`, `stuff`, `quality` | `slot`: a weapon is equipped, apparel is worn, anything else goes to the inventory. |
| `buildRoom` | `rect` (walls included), `wall`, `stuff`, `floor`, `door`, `doorDef`, `roof`, `home` | `rect`, `interior` and `center`. |
| `spawnFilth` | `def`, `cells`, or `rect` with `count` and `seed`; `thickness` | `spawned`. |
| `setWork` | `pawn`, `priorities`, `onlyWork` | The pawn's priorities above 0. |
| `removeColonists` | `keep` (names) | `removed`. |
| `speed` | `speed`: `paused`, `normal`, `fast`, `superfast`, `ultrafast` | `speed` and `tick`. |
| `camera` | `cell` or `pawn`; `zoom` (`close`, `middle`, `far`, or 11 to 60); `select` | `cell` and `rootSize`. |
| `wait` | `ticks`, `untilTick`, or `probe` with `args`, `path`, `op` (`eq`, `ne`, `lt`, `le`, `gt`, `ge`), `value`; `timeoutSeconds` | `tick`, `seconds` and the final `value`. |
| `screenshot` | `path` | `path` and `bytes`. |
| `probe` | `name`, `args` | The probe's result. |
| `listProbes` | none | All probe and command names. |
| `log` | `minLevel` (`message`, `warning`, `error`), `count`, `contains` | `errors`, `warnings` and the matching `entries`. |
| `loadErrors` | none | `errors`: every error logged from the harness's start until the first `loadErrors`. Later calls return the same list. |
| `newGame` | `mapSize`, `planetCoverage`, `seed` | Replaces the game with a fresh quick-test colony. |
| `runScenario` | `assembly`, `entryType`, `outputDirectory`, `recordFps`, `debugPanel` | Runs a scenario class from a built assembly and replies with its report. `debugPanel` shows the scenario debugger unless `recordFps` is above 0. |
| `debugger` | `action` (`state`, `pause`, `resume`, `step`), `since` | The running scenario's `scenario`, `paused`, `stepping`, `waiting`, `stepCount` and its timeline `steps` from index `since`. |
| `quit` | none | Closes the game without saving. |

## Probes

| Probe | Arguments | Result |
| --- | --- | --- |
| `filthCount` | `rect`, `defs` (both optional) | `count` and `byDef`. |
| `thingCount` | `def`; `rect` (optional) | `count` and `stackTotal`. |
| `pawn` | `pawn` | Cell, job, weapon, apparel, drafted and downed state, and work priorities. |
| `thoughts` | `pawn`; `def` (optional) | `has` and `memories`. |
| `floatMenu` | `pawn`, `cell`; `labelContains` (optional) | `enabledCount`, `disabledCount` and `options`. |
| `mapLoaded`, `tick` | none | The value. |

A mod adds probes with a class named `ArchotechProbes`. Each public static method that takes no argument, or one JSON string, registers as `<AssemblyName>.<MethodName>`. A mod that references `ArchotechHarness.dll` can call `HarnessProbes.Register(name, json => resultJson)` instead.
