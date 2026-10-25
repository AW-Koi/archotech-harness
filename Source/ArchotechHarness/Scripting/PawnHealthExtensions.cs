using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Script shortcuts that hurt, down, bed and break pawns.</summary>
public static class PawnHealthExtensions
{
    /// <summary>Adds an injury, such as HediffDefOf.Cut or a gunshot, to the first intact part of that def; throws when the pawn has none.</summary>
    public static Hediff_Injury Wound(this Pawn pawn, HediffDef injury, BodyPartDef part, float severity)
    {
        if (injury.injuryProps == null)
        {
            throw new HarnessException($"Wound({injury.defName}): {injury.defName} is not an injury; use AddHediff.");
        }

        if (!(severity > 0))
        {
            throw new HarnessException($"Wound({injury.defName}) needs a severity above 0; it has {severity}.");
        }

        return (Hediff_Injury)pawn.AddHediff(injury, part, severity);
    }

    /// <summary>Removes the first intact part of that def, as a stump that no longer bleeds unless fresh.</summary>
    public static Hediff_MissingPart RemovePart(this Pawn pawn, BodyPartDef part, bool fresh = false)
    {
        var missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, IntactPart(pawn, part));
        missing.IsFresh = fresh;
        pawn.health.AddHediff(missing);
        return missing;
    }

    /// <summary>Adds any hediff, such as HediffDefOf.WoundInfection, on the first intact part of that def or the whole body.</summary>
    public static Hediff AddHediff(this Pawn pawn, HediffDef def, BodyPartDef part = null, float? severity = null)
    {
        var hediff = HediffMaker.MakeHediff(def, pawn, part == null ? null : IntactPart(pawn, part));
        if (severity is { } value)
        {
            hediff.Severity = value;
        }

        pawn.health.AddHediff(hediff);
        return hediff;
    }

    /// <summary>Damages the pawn until it is downed, without wounds that bleed.</summary>
    public static void Down(this Pawn pawn)
    {
        if (!pawn.Downed)
        {
            HealthUtility.DamageUntilDowned(pawn, allowBleedingWounds: false);
        }
    }

    /// <summary>Puts the pawn in the bed at once, asleep and tired unless awake is set; a downed pawn becomes a patient.</summary>
    public static void PutInBed(this Pawn pawn, Building_Bed bed, bool awake = false)
    {
        if (!awake && pawn.needs?.rest is { } rest)
        {
            rest.CurLevel = 0.1f;
        }

        pawn.jobs.Notify_TuckedIntoBed(bed);
    }

    /// <summary>Starts the mental state now, such as MentalStateDefOf.Berserk; false when the pawn can't have it.</summary>
    public static bool StartMentalState(this Pawn pawn, MentalStateDef state)
    {
        var started = pawn.mindState.mentalStateHandler.TryStartMentalState(state, forced: true, forceWake: true);
        ScriptSession.Current?.Timeline.Add("pawn", $"{pawn.LabelShort} {(started ? "started" : "could not start")} {state.defName}");
        return started;
    }

    private static BodyPartRecord IntactPart(Pawn pawn, BodyPartDef part) =>
        pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def == part)
        ?? throw new HarnessException($"{pawn.LabelShort} has no intact {part.defName}.");
}
