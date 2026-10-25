using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Decorations from Decorate.Room(room): choose at least one, then call Build().</summary>
public sealed class DecorationBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly HarnessRoom _room;
    private readonly string _label;
    private ThingDef _plant;
    private bool _lamps;
    private ThingDef _sculpture;
    private ThingDef _sculptureStuff;

    internal DecorationBuilder(HarnessRoom room, string caller)
    {
        _room = room ?? throw new HarnessException("Decorate.Room() needs a room built with Build.Room().");
        _label = $"Decorate.Room() at {caller}";
        _token = ScriptSession.Current?.Builders.Track($"{_label} ends with .Build()");
    }

    /// <summary>Puts a pot of the plant, fully grown, in each free corner, or one by a wall when no corner is free.</summary>
    public DecorationBuilder PlantPots(ThingDef plant)
    {
        _plant = plant;
        return this;
    }

    /// <summary>Spreads standing lamps along the walls until the whole room is lit.</summary>
    public DecorationBuilder Lamps()
    {
        _lamps = true;
        return this;
    }

    /// <summary>Adds one sculpture of this def and stuff against a free wall.</summary>
    public DecorationBuilder Art(ThingDef sculpture, ThingDef stuff)
    {
        _sculpture = sculpture;
        _sculptureStuff = stuff;
        return this;
    }

    /// <summary>Best-effort: pieces that find no free floor are left out and noted; throws when nothing is chosen.</summary>
    public FurnishedRoom Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_plant == null && !_lamps && _sculpture == null)
        {
            throw new HarnessException("Decorate.Room() needs PlantPots, Lamps or Art.");
        }

        var map = World.Map;
        var grid = RoomSurvey.Grid(_room, map);
        var notes = new PresetNotes();
        var things = new List<Thing>();
        if (_sculpture != null)
        {
            var art = DecorLayout.Art(grid, _sculpture.size, _sculpture.rotatable);
            if (art != null)
            {
                things.Add(RoomSurvey.Place(_sculpture, _sculptureStuff, art, map));
            }

            notes.Count("sculptures", 1, art == null ? 0 : 1);
        }

        if (_plant != null)
        {
            var wanted = System.Math.Max(1, grid.FreeCorners().Count);
            var pots = DecorLayout.Pots(grid, wanted, _room.Center);
            things.AddRange(pots.Select(c => RoomSurvey.PlacePot(_plant, c, map)));
            notes.Count("plant pots", wanted, pots.Count);
        }

        if (_lamps)
        {
            var radius = ThingDefOf.StandingLamp.GetCompProperties<CompProperties_Glower>().glowRadius / 2f;
            var lamps = DecorLayout.Lamps(grid, ThingDefOf.StandingLamp.size, radius);
            things.AddRange(lamps.Select(l => RoomSurvey.Place(ThingDefOf.StandingLamp, null, l, map)));
            notes.Count("lamps", 1, lamps.Count);
        }

        PresetNote.Record(_label, notes);
        return new FurnishedRoom(_room, things);
    }
}
