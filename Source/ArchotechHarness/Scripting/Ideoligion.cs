using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Pins the colony's ideoligion, so a scenario's people believe the same things every run. Needs Ideology.</summary>
public static class Ideoligion
{
    /// <summary>A new ideoligion built on the memes, the same for the same seed, followed by the colony and every colonist on
    /// the map; colonists spawned later follow it too.</summary>
    public static Ideo ForColony(int seed, params string[] memes)
    {
        if (!ModsConfig.IdeologyActive)
            throw new HarnessException("Ideoligion.ForColony needs Ideology; mark the scenario [Dlc(\"ideology\")].");

        var colony = Faction.OfPlayer;
        var forced = memes.Select(meme => DefDatabase<MemeDef>.GetNamed(meme)).ToList();
        Rand.PushState(seed);
        try
        {
            colony.ideos.ChooseOrGenerateIdeo(new IdeoGenerationParms(colony.def, forcedMemes: forced));
        }
        finally
        {
            Rand.PopState();
        }

        var ideo = colony.ideos.PrimaryIdeo;
        foreach (var pawn in World.Map.mapPawns.FreeColonistsSpawned.ToList())
            pawn.ideo?.SetIdeo(ideo);

        return ideo;
    }

    /// <summary>Makes the precept the ideoligion's stance on its issue, in place of the one it held.</summary>
    public static Ideo Holding(this Ideo ideo, string precept)
    {
        var def = DefDatabase<PreceptDef>.GetNamed(precept);
        foreach (var held in ideo.PreceptsListForReading.Where(held => held.def.issue == def.issue).ToList())
            ideo.RemovePrecept(held, replacing: true);

        ideo.AddPrecept(PreceptMaker.MakePrecept(def), init: true);
        return ideo;
    }
}
