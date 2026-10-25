using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>What a building placed on exact cells replaces: buildings other than conduits, items, plants and filth.</summary>
public static class SpawnConflict
{
    public static bool Replaces(ThingDef existing) =>
        existing.category is ThingCategory.Item or ThingCategory.Plant or ThingCategory.Filth
        || existing.category == ThingCategory.Building && existing.building is not { isPowerConduit: true };

    /// <summary>Clears the cells for a new thing: destroys what it replaces and swaps impassable ground for soil.</summary>
    public static void ClearFor(CellRect cells, Map map)
    {
        foreach (var cell in cells)
        {
            foreach (var thing in cell.GetThingList(map).FindAll(t => Replaces(t.def) && t.def.destroyable))
            {
                thing.Destroy();
            }

            if (cell.GetTerrain(map).passability == Traversability.Impassable)
            {
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            }
        }
    }
}
