using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class GameErrorTallyTests
{
    private const string DrawingJanitor = "Exception drawing Janitor: System.NullReferenceException";
    private const string TickingMop = "Exception ticking Mop: System.InvalidOperationException";

    [Fact]
    public void Distinct_IdenticalMessages_CountAsOneWithTheirTotal()
    {
        var tally = new GameErrorTally();
        tally.Add(DrawingJanitor);
        tally.Add(DrawingJanitor);
        tally.Add(DrawingJanitor);

        var error = Assert.Single(tally.Distinct);
        Assert.Equal(new GameError(DrawingJanitor, 3), error);
    }

    [Fact]
    public void Distinct_JobFaultedLinesDifferingOnlyInTheJobNumber_CountAsOneKeepingTheFirstText()
    {
        var tally = new GameErrorTally();
        tally.Add("Job 12 faulted: render failed");
        tally.Add("Job 4087 faulted: render failed");

        var error = Assert.Single(tally.Distinct);
        Assert.Equal(new GameError("Job 12 faulted: render failed", 2), error);
    }

    [Fact]
    public void Distinct_DifferentMessages_KeepTheOrderTheyFirstAppeared()
    {
        var tally = new GameErrorTally();
        tally.Add(TickingMop);
        tally.Add(DrawingJanitor);
        tally.Add(TickingMop);

        Assert.Equal([new GameError(TickingMop, 2), new GameError(DrawingJanitor, 1)], tally.Distinct);
    }

    [Fact]
    public void FailureMessage_WithDisallowedErrors_NamesTheirCountAndTheFirstErrorsFirstLine()
    {
        var tally = new GameErrorTally();
        tally.Add(TickingMop + "\nat Mop.Tick()");
        tally.Add(DrawingJanitor);
        tally.Add(DrawingJanitor);

        var message = tally.FailureMessage(GameErrorAllowList.None);

        Assert.Equal($"GameErrors: the game logged 3 errors (2 distinct) during the scenario; the first: {TickingMop}", message);
    }

    [Fact]
    public void FailureMessage_WhenEveryErrorIsAllowed_IsNull()
    {
        var tally = new GameErrorTally();
        tally.Add(DrawingJanitor);

        var message = tally.FailureMessage(GameErrorAllowList.From([new AllowGameErrorsAttribute("drawing Janitor")]));

        Assert.Null(message);
    }

    [Fact]
    public void FailureMessage_WithoutErrors_IsNull()
    {
        Assert.Null(new GameErrorTally().FailureMessage(GameErrorAllowList.None));
    }
}
