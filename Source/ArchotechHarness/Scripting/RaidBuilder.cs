using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A raid from Spawn.Raid(points), fired even while the world is calm: choose its faction, strategy and arrival, then call Spawn().</summary>
public sealed class RaidBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly float _points;
    private Faction _faction;
    private RaidStrategyDef _strategy;
    private PawnsArrivalModeDef _arrival;

    internal RaidBuilder(float points, string caller)
    {
        _points = points;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Raid({points}) at {caller} ends with .Spawn()");
    }

    /// <summary>Without it, the game picks a hostile faction that can raid with these points.</summary>
    public RaidBuilder From(Faction faction)
    {
        _faction = faction;
        return this;
    }

    /// <summary>Such as RaidStrategyDefOf.ImmediateAttack; without it, the game picks one.</summary>
    public RaidBuilder Strategy(RaidStrategyDef strategy)
    {
        _strategy = strategy;
        return this;
    }

    /// <summary>Such as PawnsArrivalModeDefOf.EdgeWalkIn or CenterDrop; without it, the game picks one the strategy allows.</summary>
    public RaidBuilder Arriving(PawnsArrivalModeDef arrival)
    {
        _arrival = arrival;
        return this;
    }

    /// <summary>Fires the raid and returns its raiders; raiders in drop pods are not spawned yet. Throws when the raid does not fire.</summary>
    public IReadOnlyList<Pawn> Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (!(_points > 0))
        {
            throw new HarnessException($"Spawn.Raid needs more than 0 points; it has {_points}.");
        }

        var map = World.Map;
        var parms = new IncidentParms
        {
            target = map,
            points = _points,
            faction = _faction,
            raidStrategy = _strategy,
            raidArrivalMode = _arrival,
            forced = true,
        };
        var lordsBefore = map.lordManager.lords.ToHashSet();
        if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
        {
            throw new HarnessException(
                $"The raid of {_points} points did not fire: no {_faction?.Name ?? "hostile faction"} could raid with {_strategy?.defName ?? "any strategy"} arriving by {_arrival?.defName ?? "any arrival"}.");
        }

        var raiders = map.lordManager.lords
            .Where(lord => !lordsBefore.Contains(lord) && lord.faction == parms.faction)
            .SelectMany(lord => lord.ownedPawns)
            .ToList();
        ScriptSession.Current?.Timeline.Add("raid",
            $"{raiders.Count} {parms.faction.Name} raiders, {parms.raidStrategy.defName}, {parms.raidArrivalMode.defName}, {parms.points:F0} points");
        return raiders;
    }
}
