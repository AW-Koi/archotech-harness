## What this changes

Describe what the change does and why the harness needs it.

## How you verified it

List the unit tests you ran (`dotnet test ArchotechHarness.sln`) and the scenarios you ran with `./archo test scenarios`, with their results.

## Game version

Name the RimWorld version you ran the scenarios against.

## Impact on scenario authors

Say whether existing scenarios, the scenario API or `archo` commands change, and how authors update.

## Checklist

- [ ] I wrote the failing test first, and it passes now.
- [ ] InspectCode reports no warnings in the files I touched.
- [ ] Each file holds one top-level type, named after it.
- [ ] Scenarios and the scenario API use typed defs, not defName strings.
- [ ] The change adds no game files and no paths specific to my machine.
- [ ] Prose and commit messages use British English in the active voice.
