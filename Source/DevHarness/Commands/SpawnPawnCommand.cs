using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace DevHarness.Commands;

/// <summary>Generates a colonist able to do the requested work, with chosen traits and skills.</summary>
public static class SpawnPawnCommand
{
    private const int GenerationAttempts = 100;

    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var cell = Arguments.Cell(args, "cell", map);
        var kind = Arguments.Def<PawnKindDef>(Arguments.String(args, "kind", "Colonist"));
        var work = args["work"] as JsonObject ?? new JsonObject();
        var workTypes = work.Select(entry => Arguments.Def<WorkTypeDef>(entry.Key)).ToList();

        Pawn pawn = null;
        for (var attempt = 0; attempt < GenerationAttempts && pawn == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, allowAddictions: false));
            if (workTypes.All(w => !candidate.WorkTypeIsDisabled(w)))
            {
                pawn = candidate;
            }
        }

        if (pawn == null)
        {
            throw new HarnessException($"No {kind.defName} able to do {string.Join(", ", workTypes)} in {GenerationAttempts} attempts.");
        }

        var nickname = Arguments.String(args, "name");
        if (nickname != null && pawn.Name is NameTriple full)
        {
            pawn.Name = new NameTriple(full.First, nickname, full.Last);
        }

        foreach (var traitName in Arguments.Strings(args, "traits"))
        {
            GainTrait(pawn, traitName);
        }

        if (args["skills"] is JsonObject skills)
        {
            foreach (var (skillName, level) in skills)
            {
                pawn.skills.GetSkill(Arguments.Def<SkillDef>(skillName)).Level = (int)level!.GetValue<double>();
            }
        }

        foreach (var tagName in Arguments.Strings(args, "incapableOf"))
        {
            MakeIncapableOf(pawn, (WorkTags)Enum.Parse(typeof(WorkTags), tagName), workTypes);
        }

        GenSpawn.Spawn(pawn, cell, map);
        if (work.Count > 0)
        {
            SetWorkCommand.Apply(pawn, work, Arguments.Bool(args, "onlyWork", false));
        }

        return CommandResult.Done(new JsonObject
        {
            ["id"] = pawn.ThingID,
            ["name"] = pawn.LabelShort,
            ["cell"] = Arguments.Json(pawn.Position),
            ["incapableOf"] = pawn.CombinedDisabledWorkTags.ToString(),
            ["traits"] = new JsonArray(pawn.story.traits.allTraits.Select(t => (JsonNode)t.def.defName).ToArray()),
        });
    }

    /// <summary>Swaps in a vanilla childhood that disables the tag without disabling the requested work.</summary>
    private static void MakeIncapableOf(Pawn pawn, WorkTags tag, List<WorkTypeDef> workTypes)
    {
        var childhood = DefDatabase<BackstoryDef>.AllDefs.FirstOrDefault(b =>
            b.slot == BackstorySlot.Childhood
            && (b.workDisables & tag) != 0
            && workTypes.All(w => (b.workDisables & w.workTags) == 0));
        if (childhood == null)
        {
            throw new HarnessException($"No childhood backstory disables {tag} while allowing the requested work.");
        }

        pawn.story.Childhood = childhood;
        pawn.Notify_DisabledWorkTypesChanged();
    }

    /// <summary>Adds a trait as "defName" or "defName:degree", dropping traits it conflicts with.</summary>
    private static void GainTrait(Pawn pawn, string traitName)
    {
        var parts = traitName.Split(':');
        var def = Arguments.Def<TraitDef>(parts[0]);
        var degree = parts.Length > 1 ? int.Parse(parts[1]) : def.degreeDatas.First().degree;
        foreach (var existing in pawn.story.traits.allTraits.Where(t => t.def == def || t.def.ConflictsWith(def)).ToList())
        {
            pawn.story.traits.RemoveTrait(existing);
        }

        pawn.story.traits.GainTrait(new Trait(def, degree, forced: true));
    }
}
