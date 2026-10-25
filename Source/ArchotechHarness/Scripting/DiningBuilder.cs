using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A dining preset from Furnish.Dining(room, area): choose options, then call Build().</summary>
public sealed class DiningBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly HarnessRoom _room;
    private readonly CellRect _area;
    private readonly string _label;
    private ThingDef _table;
    private ThingDef _stuff;

    internal DiningBuilder(HarnessRoom room, CellRect area, string caller)
    {
        _room = room ?? throw new HarnessException("Furnish.Dining() needs a room built with Build.Room().");
        _area = area;
        _label = $"Furnish.Dining() at {caller}";
        _token = ScriptSession.Current?.Builders.Track($"{_label} ends with .Build()");
    }

    /// <summary>Without it, ThingDefOf.Table2x2c.</summary>
    public DiningBuilder Table(ThingDef table)
    {
        _table = table;
        return this;
    }

    /// <summary>The stuff for tables and chairs; without it, each def's default.</summary>
    public DiningBuilder MadeOf(ThingDef stuff)
    {
        _stuff = stuff;
        return this;
    }

    /// <summary>Best-effort: tables whose spot isn't free are left out and noted; throws when the area leaves the room.</summary>
    public FurnishedRoom Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_table != null && _table.surfaceType != SurfaceType.Eat)
        {
            throw new HarnessException($"Furnish.Dining().Table({_table.defName}): {_table.defName} is not a table to eat at.");
        }

        var map = World.Map;
        var table = _table ?? ThingDefOf.Table2x2c;
        var sets = DiningLayout.Plan(RoomSurvey.Grid(_room, map), _area, table.size, table.rotatable);
        var things = new List<Thing>();
        foreach (var set in sets)
        {
            things.Add(RoomSurvey.Place(table, _stuff, set.Table, map));
            things.AddRange(set.Chairs.Select(chair => RoomSurvey.Place(ThingDefOf.DiningChair, _stuff, chair, map)));
        }

        var notes = new PresetNotes();
        notes.Count("tables", System.Math.Max(1, DiningLayout.Capacity(_area, table.size, table.rotatable)), sets.Count, "no free floor in the area");
        PresetNote.Record(_label, notes);
        return new FurnishedRoom(_room, things);
    }
}
