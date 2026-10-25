using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ArchotechHarness.Scripting;

/// <summary>Starts gatherings now, even while the world is calm: weddings and parties.</summary>
public static class Ceremony
{
    /// <summary>Makes the pair fiancés if they are not, and starts their marriage ceremony at the spot or a marriage site the game picks.</summary>
    /// <remarks>Throws when no site is free. The ceremony ends with the pair married.</remarks>
    public static Lord Wedding(Pawn first, Pawn second, IntVec3? spot = null)
    {
        if (first == second)
        {
            throw new HarnessException("Ceremony.Wedding needs two different pawns.");
        }

        if (!first.relations.DirectRelationExists(PawnRelationDefOf.Fiance, second))
        {
            first.relations.TryRemoveDirectRelation(PawnRelationDefOf.Lover, second);
            first.relations.AddDirectRelation(PawnRelationDefOf.Fiance, second);
        }

        var site = spot ?? (RCellFinder.TryFindMarriageSite(first, second, out var found)
            ? found
            : throw new HarnessException($"No marriage site for {first.LabelShort} and {second.LabelShort}; pass a spot."));
        var lord = LordMaker.MakeNewLord(first.Faction, new LordJob_Joinable_MarriageCeremony(first, second, site), first.Map);
        ScriptSession.Current?.Timeline.Add("ceremony", $"wedding of {first.LabelShort} and {second.LabelShort} at {site}");
        return lord;
    }

    /// <summary>Starts a party the organizer hosts at a spot the game picks; throws when the game can't start one.</summary>
    public static Lord Party(Pawn organizer)
    {
        var map = organizer.Map;
        var before = map.lordManager.lords.ToHashSet();
        if (!GatheringDefOf.Party.Worker.TryExecute(map, organizer))
        {
            throw new HarnessException($"{organizer.LabelShort} could not start a party: no gathering spot or the pawn can't host.");
        }

        ScriptSession.Current?.Timeline.Add("ceremony", $"party hosted by {organizer.LabelShort}");
        return map.lordManager.lords.First(lord => !before.Contains(lord));
    }
}
