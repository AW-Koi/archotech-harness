using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A bedroom preset from Furnish.Bedroom(room): choose options, then call Build().</summary>
public sealed class BedroomBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly HarnessRoom _room;
    private readonly string _label;
    private readonly List<Pawn> _owners = new();
    private ThingDef _bed;
    private ThingDef _stuff;
    private TerrainDef _floor;
    private bool _lamp;
    private ThingDef _plant;
    private ThingDef _sculpture;
    private ThingDef _sculptureStuff;

    internal BedroomBuilder(HarnessRoom room, string caller)
    {
        _room = room ?? throw new HarnessException("Furnish.Bedroom() needs a room built with Build.Room().");
        _label = $"Furnish.Bedroom() at {caller}";
        _token = ScriptSession.Current?.Builders.Track($"{_label} ends with .Build()");
    }

    /// <summary>Without it, ThingDefOf.Bed; a double bed works too.</summary>
    public BedroomBuilder Bed(ThingDef bed)
    {
        _bed = bed;
        return this;
    }

    /// <summary>The stuff for the bed and furniture that accept it; without it, each def's default.</summary>
    public BedroomBuilder MadeOf(ThingDef stuff)
    {
        _stuff = stuff;
        return this;
    }

    /// <summary>Lays this floor over the room before furnishing it.</summary>
    public BedroomBuilder Floor(TerrainDef floor)
    {
        _floor = floor;
        return this;
    }

    /// <summary>Adds a standing lamp near the bed.</summary>
    public BedroomBuilder Lamp()
    {
        _lamp = true;
        return this;
    }

    /// <summary>Adds potted plants of this kind in up to two free corners away from the bed.</summary>
    public BedroomBuilder Plants(ThingDef plant)
    {
        _plant = plant;
        return this;
    }

    /// <summary>Adds a sculpture of this def and stuff against a free wall.</summary>
    public BedroomBuilder Art(ThingDef sculpture, ThingDef stuff)
    {
        _sculpture = sculpture;
        _sculptureStuff = stuff;
        return this;
    }

    /// <summary>Assigns the bed to these pawns.</summary>
    public BedroomBuilder Owner(params Pawn[] owners)
    {
        _owners.AddRange(owners);
        return this;
    }

    /// <summary>Best-effort: pieces that find no free floor are left out and noted in the report.</summary>
    public FurnishedRoom Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_bed != null && !_bed.IsBed)
        {
            throw new HarnessException($"Furnish.Bedroom().Bed({_bed.defName}): {_bed.defName} is not a bed.");
        }

        var map = World.Map;
        var bedDef = _bed ?? ThingDefOf.Bed;
        if (_floor != null)
        {
            foreach (var cell in _room.Interior)
            {
                map.terrainGrid.SetTerrain(cell, _floor);
            }
        }

        var grid = RoomSurvey.Grid(_room, map);
        var notes = new PresetNotes();
        var plan = BedroomLayout.Plan(grid, bedDef.size, FurnitureDefOf.Dresser.size);
        var things = new List<Thing>();
        if (plan.Bed != null)
        {
            things.Add(RoomSurvey.Place(bedDef, _stuff, plan.Bed, map));
        }

        things.AddRange(plan.EndTables.Select(t => RoomSurvey.Place(FurnitureDefOf.EndTable, _stuff, t, map)));
        if (plan.Dresser != null)
        {
            things.Add(RoomSurvey.Place(FurnitureDefOf.Dresser, _stuff, plan.Dresser, map));
        }

        notes.Count("beds", 1, plan.Bed == null ? 0 : 1);
        notes.Count("end tables", 2, plan.EndTables.Count);
        notes.Count("dressers", 1, plan.Dresser == null ? 0 : 1);
        var nearBed = plan.Bed?.Rect.CenterCell ?? _room.Center;
        if (_lamp)
        {
            var lamp = DecorLayout.Nearest(grid, ThingDefOf.StandingLamp.size, rotatable: true, nearBed);
            if (lamp != null)
            {
                things.Add(RoomSurvey.Place(ThingDefOf.StandingLamp, null, lamp, map));
            }

            notes.Count("lamps", 1, lamp == null ? 0 : 1);
        }

        if (_plant != null)
        {
            var pots = DecorLayout.Pots(grid, 2, nearBed);
            things.AddRange(pots.Select(c => RoomSurvey.PlacePot(_plant, c, map)));
            notes.Count("plant pots", 2, pots.Count);
        }

        if (_sculpture != null)
        {
            var art = DecorLayout.Art(grid, _sculpture.size, _sculpture.rotatable);
            if (art != null)
            {
                things.Add(RoomSurvey.Place(_sculpture, _sculptureStuff, art, map));
            }

            notes.Count("sculptures", 1, art == null ? 0 : 1);
        }

        if (things.OfType<Building_Bed>().FirstOrDefault() is { } bed)
        {
            foreach (var owner in _owners)
            {
                owner.ownership.ClaimBedIfNonMedical(bed);
            }
        }

        PresetNote.Record(_label, notes);
        return new FurnishedRoom(_room, things);
    }
}
