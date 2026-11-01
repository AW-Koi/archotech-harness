using ArchotechHarness.Server;

namespace Scenarios;

/// <summary>The player's pause holds the script: a wait doesn't time out while the player keeps the game paused, the script
/// carries on at the speed the player resumes at, and unpausing a pause the script holds is noted in the timeline.</summary>
public sealed class PlayerPause : HarnessScenario
{
    private const double PauseSeconds = 4;

    private bool _released;

    public override async Task Run()
    {
        // A colonist idles in calm weather at normal speed
        World.Calm();
        var idler = Spawn.Colonist().WithName("Idler").KeepingOtherWork().Spawn();
        Colonists.RemoveAllExcept(idler);
        Clock.Speed = TimeSpeed.Normal;
        await Frames(2);
        var session = ScriptSession.Current;

        // During a two-second wait the player pauses through the time controls, and resumes at superfast four seconds later
        var wait = Until(() => _released, timeoutSeconds: 2);
        PlayerSets(session, TimeSpeed.Paused);
        var held = session.Control.Paused;
        ResumeLater(session);
        await Frames();
        _released = true;
        await wait;

        // The script waited out the player's pause without timing out, and carries on at the player's speed
        Check(held);
        Check(!session.Control.Paused);
        Check(Find.TickManager.CurTimeSpeed == TimeSpeed.Superfast);

        // The script pauses the game itself, then the player unpauses it: the script runs on, and the timeline says so
        Clock.Speed = TimeSpeed.Paused;
        await Frames(2);
        PlayerSets(session, TimeSpeed.Normal);
        Check(!session.Control.Paused);
        Check(session.Timeline.Steps.Any(step => step.Text == ScriptSession.PlayerResumedHeldPause));
        Clock.Speed = TimeSpeed.Paused;
    }

    /// <summary>Changes the speed as the time controls do when the player uses them.</summary>
    private static void PlayerSets(ScriptSession session, TimeSpeed speed)
    {
        var before = Find.TickManager.CurTimeSpeed;
        Find.TickManager.CurTimeSpeed = speed;
        session.PlayerChangedSpeed(TimeControlsPatch.ChangeOf(before, speed));
    }

    private static void ResumeLater(ScriptSession session)
    {
        var resumeAt = DateTime.UtcNow.AddSeconds(PauseSeconds);
        void Poll()
        {
            if (DateTime.UtcNow >= resumeAt)
            {
                PlayerSets(session, TimeSpeed.Superfast);
                return;
            }

            MainThreadDispatcher.RunLater(1, Poll);
        }

        MainThreadDispatcher.RunLater(1, Poll);
    }
}
