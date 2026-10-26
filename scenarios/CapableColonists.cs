namespace Scenarios;

/// <summary>Colonists spawned without IncapableOf can do every kind of work, whatever their backstories and traits roll.</summary>
public sealed class CapableColonists : HarnessScenario
{
    private const int Colonists = 30;

    public override async Task Run()
    {
        // A crowd of colonists spawned with nothing specified
        Clock.Speed = TimeSpeed.Paused;
        var crowd = Enumerable.Range(0, Colonists).Select(_ => Spawn.Colonist().Spawn()).ToList();

        // None of them came out unable to do any kind of work
        var incapable = crowd.Where(pawn => pawn.CombinedDisabledWorkTags != WorkTags.None).ToList();
        foreach (var pawn in incapable)
            Note(pawn.LabelShort, pawn.CombinedDisabledWorkTags.ToString());
        Check(incapable.Count == 0);
        await Frames(2);
    }
}
