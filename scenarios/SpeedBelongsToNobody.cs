namespace Scenarios;

/// <summary>The script's speed applies once and is never forced again: other code may change it, and the game's own
/// slow-down for threats is off while a scenario runs.</summary>
public sealed class SpeedBelongsToNobody : HarnessScenario
{
    public override async Task Run()
    {
        // The script asks for superfast, then something else pauses the game, and it stays paused
        Clock.Speed = TimeSpeed.Superfast;
        await Frames(2);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        await Frames(10);
        Check(Find.TickManager.CurTimeSpeed == TimeSpeed.Paused);

        // A threat signals the game to force normal speed, and the run keeps its pace
        Clock.Speed = TimeSpeed.Superfast;
        Find.TickManager.slower.SignalForceNormalSpeed();
        await Frames(2);
        Check(!Find.TickManager.slower.ForcedNormalSpeed);
        Check(Find.TickManager.CurTimeSpeed == TimeSpeed.Superfast);
    }
}
