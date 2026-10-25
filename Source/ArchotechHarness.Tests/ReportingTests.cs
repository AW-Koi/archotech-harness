using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class ReportingTests
{
    private const int FixedSeed = 1234;
    private const int FreshSeed = 77;

    [Fact]
    public void AttachFile_ThatDoesNotExist_Throws()
    {
        Assert.Throws<HarnessException>(() => Script.AttachFile(Path.Combine(Path.GetTempPath(), "archo-missing", "transcript.txt")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape.txt")]
    [InlineData("nested/transcript.txt")]
    public void AttachText_WithANameThatIsNotAPlainFileName_Throws(string name)
    {
        Assert.Throws<HarnessException>(() => Script.AttachText(name, "Ada: Hello."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TicksPerSecond_OverNoTime_Throws(double seconds)
    {
        Action measure = () => Script.TicksPerSecond(seconds);

        Assert.Throws<HarnessException>(measure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ColonistsSpawn_FewerThanOne_Throws(int count)
    {
        var crowd = Spawn.Colonists(count);

        Assert.Throws<HarnessException>(() => crowd.Spawn());
    }

    [Fact]
    public void SeedResolve_WithASeedAttribute_IsThatSeed()
    {
        Assert.Equal(FixedSeed, ScenarioSeed.Resolve(typeof(SeededScenario), () => FreshSeed));
    }

    [Fact]
    public void SeedResolve_WithoutASeedAttribute_IsAFreshSeed()
    {
        Assert.Equal(FreshSeed, ScenarioSeed.Resolve(typeof(UnseededScenario), () => FreshSeed));
    }

    [Seed(FixedSeed)]
    private sealed class SeededScenario;

    private sealed class UnseededScenario;
}
