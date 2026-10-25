using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Places player blueprints for colonists to build, clearing what would block them.</summary>
internal static class Blueprints
{
    public static Blueprint_Build Place(Map map, BuildableDef def, ThingDef stuff, IntVec3 cell, Rot4 rotation)
    {
        var thingDef = def as ThingDef;
        if (thingDef != null)
        {
            Commands.SpawnConflict.ClearFor(GenAdj.OccupiedRect(cell, rotation, thingDef.Size), map);
        }

        var material = thingDef is { MadeFromStuff: true } ? stuff ?? GenStuff.DefaultStuffFor(thingDef) : null;
        return GenConstruct.PlaceBlueprintForBuild(def, cell, map, rotation, Faction.OfPlayer, material);
    }
}
