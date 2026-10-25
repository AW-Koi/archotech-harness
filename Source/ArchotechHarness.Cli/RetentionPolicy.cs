using System.Globalization;

namespace ArchotechHarness.Cli;

/// <summary>How many run folders to keep, and how many bytes they may fill; null means no limit.</summary>
/// <remarks>Set through ARCHO_KEEP_RUNS ("all" for no count limit) and ARCHO_KEEP_MB ("0" for no size cap).</remarks>
public sealed record RetentionPolicy(int? KeepRuns, long? MaxBytes)
{
    public const int DefaultKeepRuns = 20;
    public const long DefaultMaxMegabytes = 1024;

    private const long Megabyte = 1024 * 1024;

    public static RetentionPolicy KeepEverything { get; } = new(null, null);

    public static RetentionPolicy FromEnvironment() =>
        Parse(Environment.GetEnvironmentVariable("ARCHO_KEEP_RUNS"), Environment.GetEnvironmentVariable("ARCHO_KEEP_MB"));

    public static RetentionPolicy Parse(string? keepRuns, string? maxMegabytes)
    {
        int? runs = keepRuns switch
        {
            null or "" => DefaultKeepRuns,
            "all" => null,
            _ => NonNegative(keepRuns, "The number of runs to keep"),
        };
        var megabytes = maxMegabytes is null or "" ? DefaultMaxMegabytes : NonNegative(maxMegabytes, "The run size cap in MB");
        return new RetentionPolicy(runs, megabytes == 0 ? null : megabytes * Megabyte);
    }

    private static int NonNegative(string value, string what) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new ScenarioException($"{what} must be a whole number of 0 or more, not '{value}'.");
}
