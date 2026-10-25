using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Sows a plant on a cell at a growth, replacing other plants there and laying soil where it can't grow.</summary>
public static class PlantSower
{
    public static Plant Sow(ThingDef plant, IntVec3 cell, Map map, float growth)
    {
        if (!plant.plant.completelyIgnoreFertility && map.fertilityGrid.FertilityAt(cell) < plant.plant.fertilityMin)
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
        }

        foreach (var other in cell.GetThingList(map).OfType<Plant>().ToList())
        {
            other.Destroy();
        }

        var sown = (Plant)ThingMaker.MakeThing(plant);
        sown.Growth = growth;
        sown.sown = true;
        return (Plant)GenSpawn.Spawn(sown, cell, map);
    }
}
