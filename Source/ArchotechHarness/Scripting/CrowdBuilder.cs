using System.Collections.Generic;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Many colonists at once from Spawn.Colonists(count), generated without retries: optionally set Within and OfKind, then call Spawn().</summary>
public sealed class CrowdBuilder
{
    private const int SpreadRadius = 12;

    private readonly PendingBuilders.Token _token;
    private readonly int _count;
    private CellRect? _area;
    private PawnKindDef _kind;

    internal CrowdBuilder(int count, string caller)
    {
        _count = count;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Colonists({count}) at {caller} ends with .Spawn()");
    }

    /// <summary>Spreads them over standable cells in the area; without it, around the map's centre.</summary>
    public CrowdBuilder Within(CellRect area)
    {
        _area = area;
        return this;
    }

    /// <summary>Without it, PawnKindDefOf.Colonist.</summary>
    public CrowdBuilder OfKind(PawnKindDef kind)
    {
        _kind = kind;
        return this;
    }

    public IReadOnlyList<Pawn> Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_count < 1)
        {
            throw new HarnessException($"Spawn.Colonists needs a count of 1 or more; it has {_count}.");
        }

        var map = World.Map;
        var cells = _area is { } area ? SpawnFilthCommand.PickCells(map, area.ClipInsideMap(map), _count, Rand.Int) : [];
        var pawns = new List<Pawn>(_count);
        for (var i = 0; i < _count; i++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(_kind ?? PawnKindDefOf.Colonist, Faction.OfPlayer,
                canGeneratePawnRelations: false, allowAddictions: false));
            var cell = i < cells.Count ? cells[i] : CellFinder.RandomClosewalkCellNear(_area?.CenterCell ?? World.Center, map, SpreadRadius);
            pawns.Add((Pawn)GenSpawn.Spawn(pawn, cell, map));
        }

        ScriptSession.Current?.Timeline.Add("crowd", $"{_count} colonists");
        return pawns;
    }
}
