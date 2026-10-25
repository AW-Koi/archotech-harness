using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Creates colonists, things and filth on the current map.</summary>
public static class Spawn
{
    /// <summary>Generates a colonist able to do the listed work; with onlyWork, every other work type is off.</summary>
    /// <remarks>Traits are new Trait(def, degree). incapableOf swaps in a childhood that disables those work tags.</remarks>
    public static Pawn Colonist(string name, IntVec3? at = null, HarnessRoom inside = null, IEnumerable<WorkTypeDef> work = null,
        IEnumerable<Trait> traits = null, bool onlyWork = true, PawnKindDef kind = null, WorkTags incapableOf = WorkTags.None) =>
        SpawnPawnCommand.Generate(World.Map, new ColonistPlan(at ?? inside?.Center ?? World.Center)
        {
            Nickname = name,
            Kind = kind ?? PawnKindDefOf.Colonist,
            Work = (work ?? []).ToDictionary(workType => workType, _ => 1),
            OnlyWork = onlyWork,
            Traits = (traits ?? []).ToList(),
            IncapableOf = incapableOf,
        });

    /// <summary>Places a new thing on or near the cell; without stuff, a thing made from stuff uses the def's default.</summary>
    public static Thing Thing(ThingDef def, IntVec3 at, int count = 1, ThingDef stuff = null, QualityCategory? quality = null) =>
        SpawnThingCommand.Place(ThingFactory.Make(def, stuff, quality), at, World.Map, count);

    /// <summary>Makes filth on the cell, or thickens filth already there; null when the cell takes no filth.</summary>
    public static Filth Filth(ThingDef def, IntVec3 at, int thickness = 1) =>
        SpawnFilthCommand.Spawn(World.Map, def, [at], thickness).FirstOrDefault();
}
