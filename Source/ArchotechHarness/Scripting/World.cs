using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The current map and queries over it.</summary>
public static class World
{
    public static Map Map => Arguments.CurrentMap();

    public static IntVec3 Center => Map.Center;

    /// <summary>Filth on the map, optionally inside an area and limited to some defNames.</summary>
    public static IReadOnlyList<Filth> Filth(CellRect? area = null, params string[] defs) =>
        Map.listerThings.ThingsInGroup(ThingRequestGroup.Filth)
            .OfType<Filth>()
            .Where(f => area == null || area.Value.Contains(f.Position))
            .Where(f => defs.Length == 0 || defs.Contains(f.def.defName))
            .ToList();

    public static IReadOnlyList<Thing> Things(string def, CellRect? area = null) =>
        Map.listerThings.ThingsOfDef(Arguments.Def<ThingDef>(def))
            .Where(t => area == null || area.Value.Contains(t.Position))
            .ToList();

    public static Pawn Pawn(string nameOrId) =>
        Map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.ThingID == nameOrId || p.LabelShort == nameOrId)
        ?? throw new HarnessException($"No spawned pawn called '{nameOrId}'.");
}
