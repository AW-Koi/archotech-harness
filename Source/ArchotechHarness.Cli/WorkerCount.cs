namespace ArchotechHarness.Cli;

/// <summary>How many games archo test runs at once: a number, or "auto" to fit the machine.</summary>
/// <remarks>Auto allows one game per 12 GB of RAM and per 4 logical processors, at most 4, and never more than the scenarios.</remarks>
public static class WorkerCount
{
    public const int AutoMaximum = 4;

    private const long MemoryPerGame = 12L << 30;
    private const int ProcessorsPerGame = 4;

    public static int Choose(string? requested, long totalMemoryBytes, int processors, int scenarios)
    {
        int workers;
        if (requested == null)
        {
            workers = 1;
        }
        else if (requested.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            workers = Math.Min(AutoMaximum, Math.Min((int)(totalMemoryBytes / MemoryPerGame), processors / ProcessorsPerGame));
        }
        else if (!int.TryParse(requested, out workers) || workers < 1)
        {
            throw new ScenarioException($"--workers takes a number or 'auto', not '{requested}'.");
        }

        return Math.Clamp(workers, 1, Math.Max(1, scenarios));
    }
}
