using ArchotechHarness.Debugger;
using ArchotechHarness.Server;

namespace Scenarios;

/// <summary>The scenario debugger: a pause stops the script without timing out its waits, the player's ticks don't count, and a step pauses again at the next await.</summary>
public sealed class DebuggerPause : HarnessScenario
{
    private const double PauseSeconds = 4;
    private const int PlayerTicks = 120;

    private bool _released;

    public override async Task Run()
    {
        // A colonist idles in calm weather while the debug panel shows
        World.Calm();
        var idler = Spawn.Colonist().WithName("Idler").KeepingOtherWork().Spawn();
        Colonists.RemoveAllExcept(idler);
        Clock.Speed = TimeSpeed.Normal;
        await Frames(5);
        var session = ScriptSession.Current;
        Check(DebugPanel.ScreenRect.width > 0);

        // A wait with a short timeout starts, then the script pauses for longer than that timeout while the player runs some ticks
        var wait = Until(() => _released, timeoutSeconds: 2);
        var pausedAt = DateTime.UtcNow;
        session.PauseScript();
        ResumeLater(session, () => { for (var i = 0; i < PlayerTicks; i++) Find.TickManager.DoSingleTick(); });
        await Frames();
        _released = true;
        await wait;

        // The script slept through the pause, the wait did not time out and the player's ticks count as paused
        Check((DateTime.UtcNow - pausedAt).TotalSeconds >= PauseSeconds);
        Check(session.Control.PausedTicks >= PlayerTicks);
        Check(session.ScriptTicks == Find.TickManager.TicksGame - session.Control.PausedTicks);

        // A step from a pause runs the script to its next await and pauses it again
        session.PauseScript();
        var pausedAgain = false;
        MainThreadDispatcher.RunLater(10, () =>
        {
            session.StepScript();
            MainThreadDispatcher.RunLater(10, () =>
            {
                pausedAgain = session.Control.Paused;
                session.ResumeScript();
            });
        });
        await Frames();
        await Frames(30);
        Check(pausedAgain);
        Check(!session.Control.Paused);
        await Shot("panel-hidden-in-shots");
        Clock.Speed = TimeSpeed.Paused;
    }

    /// <summary>Runs the action during the pause, then resumes the script once PauseSeconds have passed.</summary>
    private static void ResumeLater(ScriptSession session, Action whilePaused)
    {
        var resumeAt = DateTime.UtcNow.AddSeconds(PauseSeconds);
        whilePaused();
        void Poll()
        {
            if (DateTime.UtcNow >= resumeAt)
            {
                session.ResumeScript();
                return;
            }

            MainThreadDispatcher.RunLater(1, Poll);
        }

        MainThreadDispatcher.RunLater(1, Poll);
    }
}
