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

    /// <summary>Without it, the player's faction.</summary>
    public Faction Faction { get; init; }

    /// <summary>The biological and chronological age in years; without it, the kind's usual age.</summary>
    public float? Age { get; init; }

    public XenotypeDef Xenotype { get; init; }

    /// <summary>Childhood and adulthood backstories that replace the generated ones in their slot.</summary>
    public IReadOnlyList<BackstoryDef> Backstories { get; init; } = [];
}
