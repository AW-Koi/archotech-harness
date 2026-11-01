using ArchotechHarness.Shared;

namespace ArchotechHarness.Cli.Tests;

public sealed class ScenarioInvokerTests : IDisposable
{
    private const string ConnectionError = "An existing connection was forcibly closed by the remote host.";

    private readonly TempDirectory _run = new();

    public void Dispose() => _run.Dispose();

    [Fact]
    public void LostGame_AfterTheGameLeftItsClosedMark_SaysTheGameWasClosed()
    {
        _run.WriteFile(GameClosedMark.FileName, string.Empty);

        Assert.Equal("The game was closed while the scenario ran.", ScenarioInvoker.LostGameError(_run.Path, ConnectionError));
    }

    [Fact]
    public void LostGame_WithNoClosedMark_ReportsTheLostConnection()
    {
        Assert.Equal("Lost the game: " + ConnectionError, ScenarioInvoker.LostGameError(_run.Path, ConnectionError));
    }
}
