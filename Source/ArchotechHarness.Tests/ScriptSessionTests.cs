using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class ScriptSessionTests
{
    [Theory]
    [InlineData(ProgramState.Playing, true, true, false, true)]
    [InlineData(ProgramState.Playing, false, false, true, false)]
    [InlineData(ProgramState.Playing, true, false, false, false)]
    [InlineData(ProgramState.Playing, true, true, true, false)]
    [InlineData(ProgramState.MapInitializing, true, true, false, false)]
    [InlineData(ProgramState.Entry, false, false, false, false)]
    public void CanHoldSpeed_OnlyOnceTheGameIsPlayable_IsTrue(ProgramState state, bool worldLoaded, bool mapLoaded, bool loading, bool expected)
    {
        Assert.Equal(expected, ScriptSession.CanHoldSpeed(state, worldLoaded, mapLoaded, loading));
    }
}
