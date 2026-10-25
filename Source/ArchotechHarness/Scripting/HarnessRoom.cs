using System.Collections.Generic;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A room the harness built: its walls' bounds, the floor inside them and helpers for the floor.</summary>
public sealed class HarnessRoom(CellRect bounds)
{
    public CellRect Bounds { get; } = bounds;

    public CellRect Interior { get; } = bounds.ContractedBy(1);

    public IntVec3 Center => Bounds.CenterCell;

    /// <summary>Filth on the floor, optionally only of the given filth ThingDefs.</summary>
    public IReadOnlyList<Filth> Filth(params ThingDef[] defs) => World.Filth(Interior, defs);

    /// <summary>Spreads filth over random floor cells and returns it; the same seed gives the same cells.</summary>
    public IReadOnlyList<Filth> Scatter(ThingDef filth, int count, int seed = 1) =>
        SpawnFilthCommand.Spawn(World.Map, filth, SpawnFilthCommand.PickCells(World.Map, Interior, count, seed));

    public bool Contains(Thing thing) => Bounds.Contains(thing.Position);
}
