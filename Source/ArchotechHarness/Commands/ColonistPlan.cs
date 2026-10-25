using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>What spawnPawn generates: work maps each work type to its priority.</summary>
public sealed class ColonistPlan(IntVec3 cell)
{
    public IntVec3 Cell { get; } = cell;

    public string Nickname { get; init; }

    public PawnKindDef Kind { get; init; } = PawnKindDefOf.Colonist;

    public IReadOnlyDictionary<WorkTypeDef, int> Work { get; init; } = new Dictionary<WorkTypeDef, int>();

    public bool OnlyWork { get; init; }

    public IReadOnlyList<Trait> Traits { get; init; } = [];

    public IReadOnlyDictionary<SkillDef, int> Skills { get; init; } = new Dictionary<SkillDef, int>();

    public WorkTags IncapableOf { get; init; }
}
