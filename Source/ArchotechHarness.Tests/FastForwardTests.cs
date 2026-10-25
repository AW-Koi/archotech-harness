using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class FastForwardTests
{
    [Fact]
    public void GameTickLoop_WithoutAFastForward_Runs()
    {
        Assert.True(FastForwardTickPatch.Prefix());
    }
}
