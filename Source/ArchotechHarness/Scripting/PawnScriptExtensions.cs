using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Script shortcuts on pawns: gear, work priorities and the current job.</summary>
public static class PawnScriptExtensions
{
    /// <summary>Gives the pawn a new thing: a weapon is equipped, apparel is worn, anything else goes to the inventory.</summary>
    public static Thing Equip(this Pawn pawn, string def, string stuff = null, string quality = null)
    {
        var arguments = new JsonObject { ["pawn"] = pawn.ThingID, ["def"] = def };
        if (stuff != null)
        {
            arguments["stuff"] = stuff;
        }

        if (quality != null)
        {
            arguments["quality"] = quality;
        }

        var id = Script.CommandNow("equip", arguments)!["id"]!.GetValue<string>();
        return pawn.equipment.AllEquipmentListForReading.Cast<Thing>()
            .Concat(pawn.apparel.WornApparel)
            .Concat(pawn.inventory.innerContainer)
            .First(t => t.ThingID == id);
    }

    public static void SetWork(this Pawn pawn, WorkTypeDef work, int priority) =>
        Script.CommandNow("setWork", new JsonObject
        {
            ["pawn"] = pawn.ThingID,
            ["priorities"] = new JsonObject { [work.defName] = priority },
        });

    /// <summary>The defName of the pawn's current job, or null when idle.</summary>
    public static string CurrentJob(this Pawn pawn) => pawn.CurJobDef?.defName;

    /// <summary>The pawn's memory thoughts, optionally only those of one ThoughtDef.</summary>
    public static IReadOnlyList<Thought_Memory> Memories(this Pawn pawn, string thoughtDef = null) =>
        (pawn.needs?.mood?.thoughts?.memories?.Memories ?? new List<Thought_Memory>())
            .Where(m => thoughtDef == null || m.def.defName == thoughtDef)
            .ToList();

    public static bool HasThought(this Pawn pawn, string thoughtDef) => pawn.Memories(thoughtDef).Count > 0;

    /// <summary>The right-click options the pawn would get at a cell, as the player would see them.</summary>
    public static IReadOnlyList<FloatMenuOption> FloatMenuAt(this Pawn pawn, IntVec3 cell) =>
        FloatMenuMakerMap.GetOptions(new List<Pawn> { pawn }, cell.ToVector3Shifted(), out _);
}
