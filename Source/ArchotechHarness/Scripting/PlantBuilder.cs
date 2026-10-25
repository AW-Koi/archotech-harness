using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A plant in the ground, such as a tree or flower, from Spawn.Plant(def): set At, then call Spawn().</summary>
public sealed class PlantBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly ThingDef _def;
    private IntVec3? _cell;
    private float _growth = 1f;

    internal PlantBuilder(ThingDef def, string caller)
    {
        _def = def;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Plant({def.defName}) at {caller} ends with .Spawn()");
    }

    public PlantBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>From 0 to 1; without it, fully grown.</summary>
    public PlantBuilder Grown(float growth)
    {
        _growth = growth;
        return this;
    }

    /// <summary>Replaces whatever is on the cell, pawns aside, and lays soil where the plant can't grow.</summary>
    public Plant Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_def.category != ThingCategory.Plant)
        {
            throw new HarnessException($"Spawn.Plant({_def.defName}): {_def.defName} is not a plant.");
        }

        if (_cell is not { } cell)
        {
            throw new HarnessException($"Spawn.Plant({_def.defName}) needs At(cell).");
        }

        if (!(_growth is >= 0f and <= 1f))
        {
            throw new HarnessException($"Spawn.Plant({_def.defName}).Grown takes a growth from 0 to 1; it has {_growth}.");
        }

        var map = World.Map;
        SpawnConflict.ClearFor(CellRect.SingleCell(cell), map);
        return PlantSower.Sow(_def, cell, map, _growth);
    }
}
