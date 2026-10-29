using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class SpeedRequestTests
{
    private readonly SpeedRequest _request = new();

    [Fact]
    public void RequestedSpeed_WhenTheGameCanTakeIt_IsDueOnce()
    {
        _request.Ask(TimeSpeed.Superfast);

        Assert.Equal(TimeSpeed.Superfast, _request.TakeDue(canApply: true));
        Assert.Null(_request.TakeDue(canApply: true));
    }

    [Fact]
    public void RequestedSpeed_WhileTheGameIsLoading_WaitsUntilItCanTakeIt()
    {
        _request.Ask(TimeSpeed.Paused);

        Assert.Null(_request.TakeDue(canApply: false));
        Assert.Equal(TimeSpeed.Paused, _request.TakeDue(canApply: true));
    }

    [Fact]
    public void LastSpeed_AskedAgainAfterBeingTaken_IsDueAgain()
    {
        _request.Ask(TimeSpeed.Fast);
        _request.TakeDue(canApply: true);

        _request.AskAgain();

        Assert.Equal(TimeSpeed.Fast, _request.TakeDue(canApply: true));
        Assert.Equal(TimeSpeed.Fast, _request.Last);
    }

    [Fact]
    public void Request_NeverAsked_HasNothingDueOrToAskAgain()
    {
        _request.AskAgain();

        Assert.Null(_request.TakeDue(canApply: true));
        Assert.Null(_request.Last);
    }
}
