using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class ClockTests
{
    private const int TicksPerHour = 2500;

    [Theory]
    [InlineData(6 * TicksPerHour, 9, 3 * TicksPerHour)]
    [InlineData(6 * TicksPerHour + 100, 9, 3 * TicksPerHour - 100)]
    [InlineData(14 * TicksPerHour, 9, 19 * TicksPerHour)]
    [InlineData(9 * TicksPerHour + 700, 9, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(23 * TicksPerHour, 0, TicksPerHour)]
    public void TicksUntilHour_FromATimeOfDay_ReachesTheHourTodayOrTomorrow(int dayTick, int hour, int expected)
    {
        Assert.Equal(expected, Clock.TicksUntilHour(dayTick, hour));
    }
}
