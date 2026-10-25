using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class ScriptControlTests
{
    private static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);

    private readonly ManualClock _clock = new();
    private readonly ScriptControl _control;

    public ScriptControlTests() => _control = new ScriptControl(() => _clock.Now, () => _clock.Tick);

    [Fact]
    public void Control_PausedForFiveSeconds_CountsFiveSecondsPaused()
    {
        _control.Pause();
        _clock.Advance(FiveSeconds);

        Assert.Equal(FiveSeconds, _control.PausedTime);
    }

    [Fact]
    public void Control_AfterResume_StopsCountingPausedTime()
    {
        _control.Pause();
        _clock.Advance(FiveSeconds);
        _control.Resume();
        _clock.Advance(FiveSeconds);

        Assert.Equal(FiveSeconds, _control.PausedTime);
    }

    [Fact]
    public void Control_TicksThePlayerRunsWhilePaused_CountAsPausedTicks()
    {
        _control.Pause();
        _clock.Advance(FiveSeconds, ticks: 300);
        _control.Resume();
        _clock.Advance(FiveSeconds, ticks: 60);

        Assert.Equal(300, _control.PausedTicks);
    }

    [Fact]
    public void Control_PausedTwice_KeepsTheFirstPauseStart()
    {
        _control.Pause();
        _clock.Advance(FiveSeconds);
        _control.Pause();
        _clock.Advance(FiveSeconds);

        Assert.Equal(TimeSpan.FromSeconds(10), _control.PausedTime);
    }

    [Fact]
    public void Control_SteppingOnceTheScriptRuns_PausesAgain()
    {
        _control.Pause();
        _control.Step();
        _control.ContinuationsRan(1);

        Assert.True(_control.Paused);
    }

    [Fact]
    public void Control_SteppingWhileTheScriptWaits_KeepsRunning()
    {
        _control.Pause();
        _control.Step();
        _control.ContinuationsRan(0);

        Assert.False(_control.Paused);
    }

    [Fact]
    public void Control_ResumedAfterAStep_RunsOnAfterTheScriptRuns()
    {
        _control.Pause();
        _control.Step();
        _control.Resume();
        _control.ContinuationsRan(1);

        Assert.False(_control.Paused);
    }
}
