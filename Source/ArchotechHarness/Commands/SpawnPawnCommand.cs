using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ArchotechHarness.Shared;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Generates a colonist able to do the requested work, with chosen traits and skills.</summary>
public static class SpawnPawnCommand
{
    private const int GenerationAttempts = 100;

    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var pawn = Generate(map, new ColonistPlan(Arguments.Cell(args, "cell", map))
        {
            Nickname = Arguments.String(args, "name"),
            Kind = Arguments.Def<PawnKindDef>(Arguments.String(args, "kind", PawnKindDefOf.Colonist.defName)),
            Work = Levels<WorkTypeDef>(args["work"]),
            OnlyWork = Arguments.Bool(args, "onlyWork", false),
            Traits = Arguments.Strings(args, "traits").Select(ParseTrait).ToList(),
            Skills = Levels<SkillDef>(args["skills"]),
            IncapableOf = Arguments.Strings(args, "incapableOf")
                .Aggregate(WorkTags.None, (tags, name) => tags | Enum.Parse<WorkTags>(name, ignoreCase: true)),
        });

        return CommandResult.Done(new JsonObject
        {
            ["id"] = pawn.ThingID,
            ["name"] = pawn.LabelShort,
            ["cell"] = Arguments.Json(pawn.Position),
            ["incapableOf"] = pawn.CombinedDisabledWorkTags.ToString(),
            ["traits"] = new JsonArray(pawn.story.traits.allTraits.Select(t => (JsonNode)t.def.defName).ToArray()),
        });
    }

    /// <summary>Generates and spawns the colonist, retrying until it can do every requested work type.</summary>
    public static Pawn Generate(Map map, ColonistPlan plan)
    {
        var workTypes = plan.Work.Keys.ToList();
        Pawn pawn = null;
        for (var attempt = 0; attempt < GenerationAttempts && pawn == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(Request(plan));
            if (CanDoAllWork(candidate, plan) && workTypes.All(w => !candidate.WorkTypeIsDisabled(w)))
            {
                pawn = candidate;
            }
        }

        if (pawn == null)
        {
            throw new HarnessException($"No {plan.Kind.defName} able to do all work{(workTypes.Count > 0 ? ", " + string.Join(", ", workTypes) + " included," : string.Empty)} in {GenerationAttempts} attempts.");
        }

        if (plan.Nickname != null && pawn.Name is NameTriple full)
        {
            pawn.Name = new NameTriple(full.First, plan.Nickname, full.Last);
        }

        foreach (var backstory in plan.Backstories)
        {
            if (backstory.slot == BackstorySlot.Childhood)
            {
                pawn.story.Childhood = backstory;
            }
            else
            {
                pawn.story.Adulthood = backstory;
            }
        }

        if (plan.Backstories.Count > 0)
        {
            pawn.Notify_DisabledWorkTypesChanged();
        }

        foreach (var trait in plan.Traits)
        {
            GainTrait(pawn, trait.def, trait.Degree);
        }

        foreach (var (skill, level) in plan.Skills)
        {
            pawn.skills.GetSkill(skill).Level = level;
        }

        if (plan.IncapableOf != WorkTags.None)
        {
            MakeIncapableOf(pawn, plan.IncapableOf, workTypes);
        }

        GenSpawn.Spawn(pawn, plan.Cell, map);
        if (plan.Work.Count > 0)
        {
            SetWorkCommand.Apply(pawn, plan.Work, plan.OnlyWork);
        }

        return pawn;
    }

    /// <summary>Whether the roll left all work enabled, apart from what a requested xenotype's genes disable.</summary>
    private static bool CanDoAllWork(Pawn candidate, ColonistPlan plan)
    {
        var allowed = WorkTags.None;
        if (plan.Xenotype != null && candidate.genes != null)
        {
            foreach (var gene in candidate.genes.GenesListForReading)
            {
                allowed |= gene.def.disabledWorkTags;
            }
        }

        return (candidate.story.DisabledWorkTagsBackstoryTraitsAndGenes & ~allowed) == WorkTags.None;
    }

    private static PawnGenerationRequest Request(ColonistPlan plan)
    {
        var request = new PawnGenerationRequest(plan.Kind, plan.Faction ?? Faction.OfPlayer, forceGenerateNewPawn: true,
            canGeneratePawnRelations: false, allowAddictions: false, forcedXenotype: plan.Xenotype);
        if (plan.Age is { } years)
        {
            request.FixedBiologicalAge = years;
            request.FixedChronologicalAge = years;
            request.AllowedDevelopmentalStages = Scripting.LifeStage.ForAge(years);
        }

        return request;
    }

    /// <summary>Reads {"defName": number} into defs and numbers.</summary>
    private static Dictionary<T, int> Levels<T>(JsonNode node) where T : Def =>
        (node as JsonObject ?? new JsonObject())
        .ToDictionary(entry => Arguments.Def<T>(entry.Key), entry => (int)Arguments.Number(entry.Value!));

    /// <summary>Reads "defName" or "defName:degree"; without a degree, the trait's first degree.</summary>
    private static Trait ParseTrait(string traitName)
    {
        var parts = traitName.Split(':');
        var def = Arguments.Def<TraitDef>(parts[0]);
        return new Trait(def, parts.Length > 1 ? int.Parse(parts[1]) : def.degreeDatas.First().degree);
    }

    /// <summary>Swaps in one childhood that disables every requested tag without disabling the requested work.</summary>
    private static void MakeIncapableOf(Pawn pawn, WorkTags tags, List<WorkTypeDef> workTypes)
    {
        var needed = workTypes.Aggregate(WorkTags.None, (all, w) => all | w.workTags);
        var childhood = FlagCover.FirstCovering(
            DefDatabase<BackstoryDef>.AllDefs.Where(b => b.slot == BackstorySlot.Childhood).ToArray(),
            b => (long)b.workDisables, (long)tags, (long)needed);
        if (childhood == null)
        {
            throw new HarnessException($"No childhood backstory disables {tags} while allowing the requested work.");
        }

        pawn.story.Childhood = childhood;
        pawn.Notify_DisabledWorkTypesChanged();
    }

    /// <summary>Adds a new forced trait of that def and degree, dropping traits it conflicts with.</summary>
    private static void GainTrait(Pawn pawn, TraitDef def, int degree)
    {
        if (def.degreeDatas.All(d => d.degree != degree))
        {
            throw new HarnessException(
                $"{def.defName} has no degree {degree}. Use one of {string.Join(", ", def.degreeDatas.Select(d => d.degree))}.");
        }

        foreach (var existing in pawn.story.traits.allTraits.Where(t => t.def == def || t.def.ConflictsWith(def)).ToList())
        {
            pawn.story.traits.RemoveTrait(existing);
        }

        pawn.story.traits.GainTrait(new Trait(def, degree, forced: true));
    }
}
