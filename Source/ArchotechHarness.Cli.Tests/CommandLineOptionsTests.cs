namespace ArchotechHarness.Cli.Tests;

public sealed class CommandLineOptionsTests
{
    [Fact]
    public void Options_NoPanel_IsAFlagThatTakesNoValue()
    {
        var options = new CommandLineOptions(["test", "--no-panel", "scenarios"]);

        Assert.True(options.Flag("--no-panel"));
        Assert.Equal(["test", "scenarios"], options.Positionals);
    }

    [Fact]
    public void Options_Film_IsAFlagThatTakesNoValue()
    {
        var options = new CommandLineOptions(["run", "--film", "Scenario.cs"]);

        Assert.True(options.Flag("--film"));
        Assert.Equal(["run", "Scenario.cs"], options.Positionals);
    }
}
