using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Lays power conduits owned by the player, skipping cells that already carry one.</summary>
public static class PowerConduits
{
    public static void Lay(Map map, IEnumerable<IntVec3> cells, ThingDef conduit)
    {
        foreach (var cell in cells.Where(c => c.InBounds(map) && !c.GetThingList(map).Any(t => t.def.building is { isPowerConduit: true })))
        {
            var thing = ThingMaker.MakeThing(conduit);
            thing.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(thing, cell, map, WipeMode.FullRefund);
        }
    }
}
