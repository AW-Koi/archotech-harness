# Contributing

## Build and test

```sh
dotnet build ArchotechHarness.sln
dotnet test ArchotechHarness.sln
```

The builds find RimWorld through `build/FindRimWorld.props`: `RIMWORLD_DIR`, else the Steam library that lists app 294100. Never commit game files; reference game assemblies by `HintPath` only.

Run the sample scenarios in game with `./archo test scenarios`. Set `ARCHO_REBUILD=1` to rebuild `archo` and the mod after you change them.

## Sending a change

1. Fork the repo and create a branch from `main`.
2. Write the failing test first, then the change, and keep every file to one top-level type.
3. Open a pull request against `main` and fill in the template.
4. The maintainer reviews it. Resolve every review conversation and push fixes to the same branch.
5. The maintainer squash-merges the pull request, so its title becomes the commit subject on `main`.

Nobody pushes to `main` directly.

## How it fits together

| Part | Where | Does |
| --- | --- | --- |
| `archo` | `Source/ArchotechHarness.Cli` | Finds and builds scenarios, writes each game's profile, launches and stops games, runs the worker pool and writes the reports. |
| Harness mod | `Source/ArchotechHarness`, `mod/` | Serves JSON commands on a loopback port and runs them on the main thread from a `Root.Update` postfix. Loads scenario assemblies and runs them through a main-thread `SynchronizationContext`. |
| Scenario API | `Source/ArchotechHarness/Scripting` | Builders and helpers that scenarios call. They share their cores with the JSON commands. |
| Scenarios props | `build/ArchotechScenarios.props` | Turns a .csproj into a scenarios project. |
| Shared | `Source/Shared` | Code compiled into both the CLI and the mod. |

`archo` keeps per-checkout state in `.harness/`: one profile per game (`profile/`, `profiles/w1`...), run folders (`runs/`, `test-runs/`) and the last session's `Player.log`. `RunRetention` prunes the run folders before each run. Profiles hold RimWorld's texture cache, and later games reuse them. It keeps machine-wide state in `%LOCALAPPDATA%/ArchotechHarness` (or `ARCHO_STATE`): live sessions, the player-file backup, the list of Mods-folder junctions and a lock. The first game takes the backup and the last one restores it.

## Game launch

`archo` starts `RimWorld.exe` with:

| Flag | Why |
| --- | --- |
| `-quicktest` | Loads straight into a Crashlanded colony on a 250 by 250 map. |
| `-savedatafolder=<profile>` | Keeps the game's config and saves out of the player's. |
| `-WindowSize=W,H` | Sets the window size. |
| `-harnessPort=N` | The port the harness mod listens on. |
| `-NoCrashDialog`, `-DisableLogStackTraces` | Keeps crashes and logs unattended. |
| `-NoWindow -NoFileLog` | Worker games only. `-NoWindow` still renders, so screenshots work. |

`-Headless` also runs scenarios, but it renders nothing, so screenshots fail. The written Prefs.xml turns on dev mode, a window, running unfocused and silence.

## Icons

`art/make_icon.py` draws the icons and needs Python with Pillow:

- `python art/make_icon.py png mod/About/ModIcon.png 128` draws the mod list icon.
- `python art/make_icon.py ico art/ArchotechHarness.ico` draws the window and taskbar icon.
