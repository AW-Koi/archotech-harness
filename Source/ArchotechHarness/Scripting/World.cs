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

    /// <summary>Filth on the map, optionally inside an area and only of the given filth ThingDefs.</summary>
    public static IReadOnlyList<Filth> Filth(CellRect? area = null, params ThingDef[] defs) =>
        Map.listerThings.ThingsInGroup(ThingRequestGroup.Filth)
            .OfType<Filth>()
            .Where(f => area == null || area.Value.Contains(f.Position))
            .Where(f => defs.Length == 0 || defs.Contains(f.def))
            .ToList();

    public static IReadOnlyList<Thing> Things(ThingDef def, CellRect? area = null) =>
        Map.listerThings.ThingsOfDef(def)
            .Where(t => area == null || area.Value.Contains(t.Position))
            .ToList();

    /// <summary>A spawned pawn by its short name (nickname) or ThingID.</summary>
    public static Pawn Pawn(string name) =>
        Map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.ThingID == name || p.LabelShort == name)
        ?? throw new HarnessException($"No spawned pawn called '{name}'.");
}
