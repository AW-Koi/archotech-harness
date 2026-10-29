namespace Scenarios;

/// <summary>A scenario pins the colony's ideoligion: the memes it asks for, the precepts it holds, the same for the same seed,
/// believed by everyone already in the colony and everyone who joins.</summary>
[Dlc("ideology")]
public sealed class PinnedIdeoligion : HarnessScenario
{
    private const int Seed = 4127;

    public override async Task Run()
    {
        // A colonist already living here before the colony's beliefs are pinned
        Clock.Speed = TimeSpeed.Paused;
        var earlier = Spawn.Colonist().WithName("Mara").Spawn();

        // The colony believes in a collective, and holds that lovin' is free
        var first = Ideoligion.ForColony(Seed, "Collectivist").Holding("Lovin_Free");
        Note("ideoligion", first.name + ": " + string.Join(", ", first.PreceptsListForReading.Select(precept => precept.def.defName)));
        Check(Faction.OfPlayer.ideos.PrimaryIdeo == first);
        Check(first.memes.Contains(DefDatabase<MemeDef>.GetNamed("Collectivist")));
        Check(first.PreceptsListForReading.Count(precept => precept.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")) == 1);
        Check(first.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_Free")));

        // The colonist already here and one who joins now both follow it
        var later = Spawn.Colonist().WithName("Kai").Spawn();
        Check(earlier.Ideo == first);
        Check(later.Ideo == first);

        // The same seed and memes make the same ideoligion again
        var name = first.name;
        var again = Ideoligion.ForColony(Seed, "Collectivist");
        Check(again.name == name);
        await Frames(2);
    }
}
