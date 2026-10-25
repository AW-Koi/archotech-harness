using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Finds and removes pawns, and the things that wake or spawn them, that could threaten a scenario's colonists.</summary>
public static class PawnSweep
{
    /// <summary>Pawns hostile to the player, dormant pawns such as ancient-danger sleepers, and pawns held in containers that aren't the player's.</summary>
    public static IReadOnlyList<Pawn> Threats(Map map)
    {
        var spawned = map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer) || p.TryGetComp<CompCanBeDormant>() is { Awake: false });
        var held = map.mapPawns.AllPawnsUnspawned.Where(p => p.Faction != Faction.OfPlayer && HolderOf(p) is { } holder && holder.Faction != Faction.OfPlayer);
        return spawned.Concat(held).Distinct().ToList();
    }

    /// <summary>Triggers, signal actions, spawners and wake-up things, and buildings of factions hostile to the player.</summary>
    public static IReadOnlyList<Thing> ThreatSources(Map map) =>
        map.listerThings.AllThings
            .Where(t => ThreatSource.IsSource(t.def) || t is Building && t.Faction is { } faction && faction.HostileTo(Faction.OfPlayer))
            .ToList();

    /// <summary>Spawned pawns in the area that are not the player's, such as wild animals, visitors and mechanoids.</summary>
    public static IReadOnlyList<Pawn> Strangers(Map map, CellRect area) =>
        map.mapPawns.AllPawnsSpawned.Where(p => area.Contains(p.Position) && p.Faction != Faction.OfPlayer).ToList();

    /// <summary>Destroys each thing, or despawns it when it can't be destroyed.</summary>
    public static void Remove(IEnumerable<Thing> things)
    {
        foreach (var thing in things.ToList())
        {
            if (thing.Destroyed)
            {
                continue;
            }

            if (thing.def.destroyable)
            {
                thing.Destroy();
            }
            else if (thing.Spawned)
            {
                thing.DeSpawn();
            }
        }
    }

    private static Thing HolderOf(Pawn pawn)
    {
        for (var holder = pawn.ParentHolder; holder != null; holder = holder.ParentHolder)
        {
            if (holder is Thing thing)
            {
                return thing;
            }
        }

        return null;
    }
}
