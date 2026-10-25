namespace ArchotechHarness.Cli.Tests;

public sealed class RetentionPolicyTests
{
    private const long Megabyte = 1024 * 1024;

    [Fact]
    public void Parse_NothingSet_KeepsTwentyRunsUnderOneGigabyte()
    {
        var policy = RetentionPolicy.Parse(keepRuns: null, maxMegabytes: null);

        Assert.Equal(new RetentionPolicy(KeepRuns: 20, MaxBytes: 1024 * Megabyte), policy);
    }

    [Fact]
    public void Parse_CountAndSize_UsesBoth()
    {
        var policy = RetentionPolicy.Parse(keepRuns: "5", maxMegabytes: "300");

        Assert.Equal(new RetentionPolicy(KeepRuns: 5, MaxBytes: 300 * Megabyte), policy);
    }

    [Fact]
    public void Parse_AllRunsAndZeroMegabytes_KeepsEverything()
    {
        var policy = RetentionPolicy.Parse(keepRuns: "all", maxMegabytes: "0");

        Assert.Equal(RetentionPolicy.KeepEverything, policy);
    }

    [Theory]
    [InlineData("-1", null)]
    [InlineData("lots", null)]
    [InlineData(null, "-5")]
    [InlineData(null, "big")]
    public void Parse_InvalidValue_Throws(string? keepRuns, string? maxMegabytes)
    {
        Assert.Throws<ScenarioException>(() => RetentionPolicy.Parse(keepRuns, maxMegabytes));
    }
}
