namespace ArchotechHarness.Cli.Tests;

public class WorkerCountTests
{
    private const long Gigabyte = 1L << 30;

    [Theory]
    [InlineData(null, 64, 32, 10, 1)]
    [InlineData("3", 64, 32, 10, 3)]
    [InlineData("3", 64, 32, 2, 2)]
    [InlineData("auto", 64, 32, 10, 4)]
    [InlineData("auto", 24, 32, 10, 2)]
    [InlineData("auto", 64, 8, 10, 2)]
    [InlineData("auto", 8, 4, 10, 1)]
    public void Choose_RequestMachineAndScenarios_GivesWorkers(string? requested, long memoryGb, int processors, int scenarios, int expected)
    {
        var workers = WorkerCount.Choose(requested, memoryGb * Gigabyte, processors, scenarios);

        Assert.Equal(expected, workers);
    }

    [Fact]
    public void Choose_NotANumber_Throws()
    {
        Assert.Throws<ScenarioException>(() => WorkerCount.Choose("many", 64 * Gigabyte, 32, 10));
    }
}
