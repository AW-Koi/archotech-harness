using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Script shortcuts for who a pawn is: titles, ideoligion roles, relations and opinions.</summary>
public static class PawnIdentityExtensions
{
    private const int OpinionLimit = 100;

    /// <summary>Grants a royal title of the faction, the Empire without one, with no rewards or letter. Needs Royalty.</summary>
    public static void GrantTitle(this Pawn pawn, RoyalTitleDef title, Faction faction = null)
    {
        if (pawn.royalty == null)
        {
            throw new HarnessException($"GrantTitle({title.defName}) needs the Royalty DLC: add [Dlc(\"royalty\")] to the scenario.");
        }

        pawn.royalty.SetTitle(faction ?? Faction.OfEmpire, title, grantRewards: false, sendLetter: false);
    }

    /// <summary>Assigns the role of the pawn's ideoligion with this def, such as PreceptDefOf.IdeoRole_Leader. Needs Ideology.</summary>
    public static void AssignRole(this Pawn pawn, PreceptDef role)
    {
        if (pawn.Ideo == null)
        {
            throw new HarnessException($"AssignRole({role.defName}) needs a pawn with an ideoligion: add [Dlc(\"ideology\")] to the scenario.");
        }

        var precept = pawn.Ideo.RolesListForReading.FirstOrDefault(r => r.def == role)
            ?? throw new HarnessException($"{pawn.LabelShort}'s ideoligion has no {role.defName} role.");
        precept.Assign(pawn, addThoughts: false);
    }

    /// <summary>Adds a direct relation from the pawn to the other, such as PawnRelationDefOf.Spouse, Fiance, Lover, ExLover or Parent.</summary>
    public static void Relate(this Pawn pawn, Pawn other, PawnRelationDef relation)
    {
        if (pawn == other)
        {
            throw new HarnessException($"Relate({relation.defName}) needs two different pawns.");
        }

        pawn.relations.AddDirectRelation(relation, other);
    }

    /// <summary>Sets the pawn's opinion of the other, from -100 to 100, through one lasting social memory; 20 or more is a friend, -20 or less a rival.</summary>
    public static void SetOpinionOf(this Pawn pawn, Pawn other, int opinion)
    {
        if (pawn == other || opinion is < -OpinionLimit or > OpinionLimit)
        {
            throw new HarnessException($"SetOpinionOf needs two different pawns and an opinion from -100 to 100; it has {opinion}.");
        }

        var memories = pawn.needs.mood.thoughts.memories;
        memories.RemoveMemoriesOfDefWhereOtherPawnIs(HarnessDefOf.ArchoHarness_StagedOpinion, other);
        var offset = opinion - pawn.relations.OpinionOf(other);
        if (offset == 0)
        {
            return;
        }

        var memory = (Thought_MemorySocial)ThoughtMaker.MakeThought(HarnessDefOf.ArchoHarness_StagedOpinion);
        memories.TryGainMemory(memory, other);
        memory.opinionOffset = offset;
    }
}
