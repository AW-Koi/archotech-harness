using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Script shortcuts on pawns: work priorities, the current job, memories and the right-click menu.</summary>
public static class PawnScriptExtensions
{
    public static void SetWork(this Pawn pawn, WorkTypeDef work, int priority) =>
        SetWorkCommand.Apply(pawn, new Dictionary<WorkTypeDef, int> { [work] = priority }, onlyWork: false);

    /// <summary>The def of the pawn's current job, or null when idle.</summary>
    public static JobDef CurrentJob(this Pawn pawn) => pawn.CurJobDef;

    /// <summary>The pawn's memory thoughts, optionally only those of one ThoughtDef.</summary>
    public static IReadOnlyList<Thought_Memory> Memories(this Pawn pawn, ThoughtDef def = null) =>
        (pawn.needs?.mood?.thoughts?.memories?.Memories ?? [])
            .Where(m => def == null || m.def == def)
            .ToList();

    public static bool HasThought(this Pawn pawn, ThoughtDef def) => pawn.Memories(def).Count > 0;

    /// <summary>The right-click options the pawn would get at a cell, as the player would see them.</summary>
    public static IReadOnlyList<FloatMenuOption> FloatMenuAt(this Pawn pawn, IntVec3 cell) =>
        FloatMenuMakerMap.GetOptions([pawn], cell.ToVector3Shifted(), out _);
}
