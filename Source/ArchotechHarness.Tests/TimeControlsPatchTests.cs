using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class TimeControlsPatchTests
{
    [Theory]
    [InlineData(TimeSpeed.Normal, TimeSpeed.Paused, PlayerSpeedChange.Paused)]
    [InlineData(TimeSpeed.Ultrafast, TimeSpeed.Paused, PlayerSpeedChange.Paused)]
    [InlineData(TimeSpeed.Paused, TimeSpeed.Normal, PlayerSpeedChange.Resumed)]
    [InlineData(TimeSpeed.Paused, TimeSpeed.Superfast, PlayerSpeedChange.Resumed)]
    [InlineData(TimeSpeed.Normal, TimeSpeed.Fast, PlayerSpeedChange.None)]
    [InlineData(TimeSpeed.Paused, TimeSpeed.Paused, PlayerSpeedChange.None)]
    public void TimeControls_ChangingTheSpeed_ClassifiesThePlayersChange(TimeSpeed before, TimeSpeed after, PlayerSpeedChange expected)
    {
        Assert.Equal(expected, TimeControlsPatch.ChangeOf(before, after));
    }
}
