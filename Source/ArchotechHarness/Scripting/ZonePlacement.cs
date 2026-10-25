using System.Linq;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Registers a new zone over an area, taking cells from older zones and leaving out cells a building blocks.</summary>
internal static class ZonePlacement
{
    /// <summary>Throws only when no cell of the area can hold the zone.</summary>
    public static TZone Fill<TZone>(TZone zone, CellRect area, Map map, out int skipped) where TZone : Zone
    {
        var usable = area.ClipInsideMap(map).Cells.Where(c => c.GetThingList(map).All(t => t.def.CanOverlapZones)).ToList();
        skipped = area.Area - usable.Count;
        if (usable.Count == 0)
        {
            throw new HarnessException($"No cell of {area} can hold a zone: it is off the map or under buildings.");
        }

        var older = usable.Select(c => map.zoneManager.ZoneAt(c)).Where(z => z != null).Distinct().ToList();
        foreach (var cell in usable.Where(c => map.zoneManager.ZoneAt(c) != null))
        {
            map.zoneManager.ZoneAt(cell).RemoveCell(cell);
        }

        foreach (var shrunk in older.Where(z => z.Cells.Count > 0))
        {
            shrunk.CheckContiguous();
        }

        map.zoneManager.RegisterZone(zone);
        foreach (var cell in usable)
        {
            zone.AddCell(cell);
        }

        return zone;
    }
}
