using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Shells or other projectiles from Spawn.Projectile(def), fired at a cell from a direction: set At, then call Spawn().</summary>
public sealed class ProjectileBuilder
{
    private const int DefaultDistance = 60;

    private readonly PendingBuilders.Token _token;
    private readonly ThingDef _def;
    private IntVec3? _target;
    private Rot4 _from = Rot4.North;
    private int _distance = DefaultDistance;
    private float _scatter;
    private int _count = 1;

    internal ProjectileBuilder(ThingDef def, string caller)
    {
        _def = def;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Projectile({def.defName}) at {caller} ends with .Spawn()");
    }

    /// <summary>The cell to aim at; required.</summary>
    public ProjectileBuilder At(IntVec3 target)
    {
        _target = target;
        return this;
    }

    /// <summary>Launches from that direction, distance cells away or at the map edge; North and 60 cells without it.</summary>
    public ProjectileBuilder FromEdge(Rot4 direction, int distance = DefaultDistance)
    {
        _from = direction;
        _distance = distance;
        return this;
    }

    /// <summary>Each one lands on a random cell within this radius of the target; 0 without it.</summary>
    public ProjectileBuilder Scatter(float radius)
    {
        _scatter = radius;
        return this;
    }

    /// <summary>How many to fire at once; 1 without it.</summary>
    public ProjectileBuilder Count(int count)
    {
        _count = count;
        return this;
    }

    /// <summary>Launches them all now and returns them in flight. A shell def fires the projectile it loads.</summary>
    public IReadOnlyList<Projectile> Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        var projectileDef = _def.projectile != null ? _def : _def.projectileWhenLoaded;
        if (projectileDef?.projectile == null)
        {
            throw new HarnessException($"Spawn.Projectile({_def.defName}): {_def.defName} is not a projectile or a shell.");
        }

        if (_target is not { } target)
        {
            throw new HarnessException($"Spawn.Projectile({_def.defName}) needs At(cell).");
        }

        if (_scatter < 0 || _count < 1)
        {
            throw new HarnessException($"Spawn.Projectile({_def.defName}) needs a Scatter of 0 or more and a Count of 1 or more; it has {_scatter} and {_count}.");
        }

        var map = World.Map;
        var origin = MapEdge.Toward(target, _from, _distance, map.Size);
        var projectiles = ScatterCells.Around(target, _scatter, _count, Rand.Int)
            .Select(cell => cell.ClampInsideMap(map))
            .Select(cell =>
            {
                var projectile = (Projectile)GenSpawn.Spawn(projectileDef, origin, map);
                projectile.Launch(null, origin.ToVector3Shifted(), cell, cell, ProjectileHitFlags.IntendedTarget);
                return projectile;
            })
            .ToList();
        ScriptSession.Current?.Timeline.Add("projectile", $"{_count} {projectileDef.defName} at {target} from {_from.ToStringHuman()}");
        return projectiles;
    }
}
