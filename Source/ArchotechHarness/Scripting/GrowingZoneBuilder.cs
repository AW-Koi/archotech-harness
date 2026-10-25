using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A growing zone to make with Build.GrowingZone(area); set what it is Planting, then call Build().</summary>
public sealed class GrowingZoneBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly CellRect _area;
    private readonly string _label;
    private ThingDef _plant;
    private float? _growth;

    internal GrowingZoneBuilder(CellRect area, string caller)
    {
        _area = area;
        _label = $"Build.GrowingZone() at {caller}";
        _token = ScriptSession.Current?.Builders.Track($"{_label} ends with .Build()");
    }

    /// <summary>The crop the zone grows; required.</summary>
    public GrowingZoneBuilder Planting(ThingDef plant)
    {
        _plant = plant;
        return this;
    }

    /// <summary>Sows the crop on every cell at this growth, from 0 to 1. Without it, the zone starts bare.</summary>
    public GrowingZoneBuilder Grown(float growth)
    {
        _growth = growth;
        return this;
    }

    /// <summary>Lays soil where the ground is too poor for the crop and clears wild plants from the area.</summary>
    public Zone_Growing Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_plant == null)
        {
            throw new HarnessException("Build.GrowingZone() needs Planting(plant).");
        }

        if (_growth is { } growth && !(growth is >= 0f and <= 1f))
        {
            throw new HarnessException($"Grown takes a growth from 0 to 1; it has {growth}.");
        }

        var map = World.Map;
        var area = _area;
        foreach (var cell in area.ClipInsideMap(map))
        {
            if (!_plant.plant.completelyIgnoreFertility && map.fertilityGrid.FertilityAt(cell) < _plant.plant.fertilityMin)
            {
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }

            foreach (var wild in cell.GetThingList(map).OfType<Plant>().ToList())
            {
                wild.Destroy();
            }
        }

        var zone = ZonePlacement.Fill(new Zone_Growing(map.zoneManager), area, map, out var skipped);
        PresetNote.Record(_label, "zone cells", area.Area, area.Area - skipped, "off the map or under buildings");
        zone.SetPlantDefToGrow(_plant);
        if (_growth is { } sownGrowth)
        {
            foreach (var cell in zone.Cells.ToList())
            {
                PlantSower.Sow(_plant, cell, map, sownGrowth);
            }
        }

        return zone;
    }
}
