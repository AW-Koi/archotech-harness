using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>An L-shaped path from Build.Path(from, to), along x then along z: set what it is Of, then call Build().</summary>
public sealed class PathBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly IntVec3 _from;
    private readonly IntVec3 _to;
    private TerrainDef _floor;
    private int _width = 1;

    internal PathBuilder(IntVec3 from, IntVec3 to, string caller)
    {
        _from = from;
        _to = to;
        _token = ScriptSession.Current?.Builders.Track($"Build.Path() at {caller} ends with .Build()");
    }

    /// <summary>The floor to lay; required.</summary>
    public PathBuilder Of(TerrainDef floor)
    {
        _floor = floor;
        return this;
    }

    /// <summary>Without it, one cell wide.</summary>
    public PathBuilder Wide(int width)
    {
        _width = width;
        return this;
    }

    public void Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_floor == null)
        {
            throw new HarnessException("Build.Path() needs Of(floor).");
        }

        if (_width < 1)
        {
            throw new HarnessException($"Build.Path() is at least one cell wide; it has Wide({_width}).");
        }

        var map = World.Map;
        foreach (var cell in PathCells.Between(_from, _to, _width))
        {
            if (cell.InBounds(map))
            {
                map.terrainGrid.SetTerrain(cell, _floor);
            }
        }
    }
}
