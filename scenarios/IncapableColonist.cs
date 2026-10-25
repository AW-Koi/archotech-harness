namespace Scenarios;

/// <summary>A colonist made incapable of two work tags at once keeps the work it was asked to do.</summary>
public sealed class IncapableColonist : HarnessScenario
{
    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        var colonist = Spawn.Colonist()
            .WithName("Gentle")
            .Doing(WorkTypeDefOf.Cleaning)
            .IncapableOf(WorkTags.Violent | WorkTags.Animals)
            .Spawn();

        Check(colonist.WorkTagIsDisabled(WorkTags.Violent));
        Check(colonist.WorkTagIsDisabled(WorkTags.Animals));
        Check(!colonist.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning));
        Note("childhood", colonist.story.Childhood.defName);
        await Frames(2);
    }
}
