using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using ArchotechHarness.Layout;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>An L-shaped line of buildings from Build.Line(def, from, to), along x then along z, such as sandbags: call Build().</summary>
public sealed class LineBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly ThingDef _def;
    private readonly IntVec3 _from;
    private readonly IntVec3 _to;
    private ThingDef _stuff;
    private bool _blueprint;

    internal LineBuilder(ThingDef def, IntVec3 from, IntVec3 to, string caller)
    {
        _def = def;
        _from = from;
        _to = to;
        _token = ScriptSession.Current?.Builders.Track($"Build.Line({def.defName}) at {caller} ends with .Build()");
    }

    /// <summary>Without it, a building made from stuff uses the def's default stuff.</summary>
    public LineBuilder MadeOf(ThingDef stuff)
    {
        _stuff = stuff;
        return this;
    }

    /// <summary>Places blueprints for colonists to build, in place of finished buildings.</summary>
    public LineBuilder AsBlueprint()
    {
        _blueprint = true;
        return this;
    }

    /// <summary>Builds one on every cell of the line, owned by the player, replacing what is in the way, and returns them.</summary>
    public IReadOnlyList<Thing> Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_def.category != ThingCategory.Building)
        {
            throw new HarnessException($"Build.Line({_def.defName}): {_def.defName} is not a building.");
        }

        var map = World.Map;
        return PathCells.Between(_from, _to, 1)
            .Where(cell => cell.InBounds(map))
            .Select(cell => _blueprint
                ? Blueprints.Place(map, _def, _stuff, cell, Rot4.North)
                : SpawnThingCommand.PlaceExactly(ThingFactory.Make(_def, _stuff), cell, Rot4.North, map))
            .ToList();
    }
}
