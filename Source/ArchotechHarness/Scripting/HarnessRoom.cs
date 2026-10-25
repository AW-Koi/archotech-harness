using System.Collections.Generic;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A room the harness built: its walls' bounds, the floor inside them and helpers for the floor.</summary>
public sealed class HarnessRoom(CellRect bounds)
{
    public CellRect Bounds { get; } = bounds;

    public CellRect Interior { get; } = bounds.ContractedBy(1);

    public IntVec3 Center => Bounds.CenterCell;

    public IReadOnlyList<Filth> Filth(params string[] defs) => World.Filth(Interior, defs);

    /// <summary>Spreads filth over random floor cells; the same seed gives the same cells.</summary>
    public int Scatter(string filthDef, int count, int seed = 1) =>
        Script.CommandNow("spawnFilth", new JsonObject
        {
            ["def"] = filthDef,
            ["rect"] = new JsonArray(Interior.minX, Interior.minZ, Interior.Width, Interior.Height),
            ["count"] = count,
            ["seed"] = seed,
        })!["spawned"]!.GetValue<int>();

    public bool Contains(Thing thing) => Bounds.Contains(thing.Position);
}
