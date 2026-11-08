namespace ArchotechHarness.Cli.Tests;

public sealed class GameArgumentsTests
{
    private const string Profile = @"D:\harness\profile";
    private const int Port = 47801;

    [Fact]
    public void Arguments_ForATestSession_LockTheWindowToItsSize()
    {
        var options = new LaunchOptions([], [], [], new WindowSize(1600, 900), Port);

        var arguments = GameArguments.For(options, Profile, Port);

        Assert.Contains("-WindowSize=1600,900", arguments);
    }

    [Fact]
    public void Arguments_ForFilming_LeaveTheWindowFreeToGoBorderless()
    {
        var options = new LaunchOptions([], [], [], new WindowSize(1600, 900), Port, Filming: true);

        var arguments = GameArguments.For(options, Profile, Port);

        Assert.DoesNotContain(arguments, argument => argument.StartsWith("-WindowSize", StringComparison.Ordinal));
    }
}
