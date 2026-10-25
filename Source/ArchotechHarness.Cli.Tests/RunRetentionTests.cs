namespace ArchotechHarness.Cli.Tests;

public sealed class RunRetentionTests : IDisposable
{
    private const int RunBytes = 100;

    private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

    private readonly TempDirectory _temp = new();
    private readonly string _root;

    public RunRetentionTests()
    {
        _root = _temp.Combine("test-runs");
        Directory.CreateDirectory(_root);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Prune_MoreRunsThanTheLimit_RemovesTheOldest()
    {
        WriteRun("oldest", Monday);
        WriteRun("older", Monday.AddHours(1));
        WriteRun("newer", Monday.AddHours(2));
        WriteRun("newest", Monday.AddHours(3));

        var result = RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: 2, MaxBytes: null), dryRun: false);

        Assert.Equal(["older", "oldest"], result.Removed.Select(r => Path.GetFileName(r.Path)));
        Assert.Equal(["newer", "newest"], RemainingRuns());
    }

    [Fact]
    public void Prune_RunsOverTheSizeCap_RemovesOlderRunsUntilUnderTheCap()
    {
        WriteRun("oldest", Monday);
        WriteRun("older", Monday.AddHours(1));
        WriteRun("newest", Monday.AddHours(2));

        var result = RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: null, MaxBytes: 2 * RunBytes + 50), dryRun: false);

        Assert.Equal(RunBytes, result.FreedBytes);
        Assert.Equal(["newest", "older"], RemainingRuns());
    }

    [Fact]
    public void Prune_NewestRunAloneOverTheSizeCap_KeepsIt()
    {
        WriteRun("older", Monday);
        WriteRun("newest", Monday.AddHours(1));

        RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: null, MaxBytes: RunBytes / 2), dryRun: false);

        Assert.Equal(["newest"], RemainingRuns());
    }

    [Fact]
    public void Prune_KeepZeroRuns_KeepsTheNewest()
    {
        WriteRun("older", Monday);
        WriteRun("newest", Monday.AddHours(1));

        RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: 0, MaxBytes: null), dryRun: false);

        Assert.Equal(["newest"], RemainingRuns());
    }

    [Fact]
    public void Prune_DryRun_ListsTheRunsAndDeletesNothing()
    {
        WriteRun("older", Monday);
        WriteRun("newest", Monday.AddHours(1));

        var result = RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: 1, MaxBytes: null), dryRun: true);

        Assert.Equal(["older"], result.Removed.Select(r => Path.GetFileName(r.Path)));
        Assert.Equal(RunBytes, result.FreedBytes);
        Assert.Equal(["newest", "older"], RemainingRuns());
    }

    [Fact]
    public void Prune_KeepEverythingPolicy_RemovesNothing()
    {
        WriteRun("older", Monday);
        WriteRun("newest", Monday.AddHours(1));

        var result = RunRetention.Prune([_root], RetentionPolicy.KeepEverything, dryRun: false);

        Assert.Empty(result.Removed);
        Assert.Equal(["newest", "older"], RemainingRuns());
    }

    [Fact]
    public void Prune_SeveralRoots_AppliesTheLimitToEachRoot()
    {
        var singleRuns = _temp.Combine("runs");
        WriteRun("test-older", Monday);
        WriteRun("test-newest", Monday.AddHours(1));
        WriteRun("single-older", Monday.AddHours(2), singleRuns);
        WriteRun("single-newest", Monday.AddHours(3), singleRuns);

        var result = RunRetention.Prune([_root, singleRuns], new RetentionPolicy(KeepRuns: 1, MaxBytes: null), dryRun: false);

        Assert.Equal(["single-older", "test-older"], result.Removed.Select(r => Path.GetFileName(r.Path)).Order());
    }

    [Fact]
    public void Prune_MissingRoot_RemovesNothing()
    {
        var result = RunRetention.Prune([_temp.Combine("never-created")], new RetentionPolicy(KeepRuns: 0, MaxBytes: 0), dryRun: false);

        Assert.Empty(result.Removed);
    }

    [Fact]
    public void Prune_LooseFileInTheRoot_LeavesItAlone()
    {
        var notes = _temp.WriteFile("test-runs/notes.txt", "keep me");
        WriteRun("older", Monday);
        WriteRun("newest", Monday.AddHours(1));

        RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: 1, MaxBytes: null), dryRun: false);

        Assert.True(File.Exists(notes));
    }

    [Fact]
    public void Prune_RunWithAnOpenFile_ReportsItAsFailedAndPrunesTheRest()
    {
        WriteRun("locked", Monday);
        WriteRun("older", Monday.AddHours(1));
        WriteRun("newest", Monday.AddHours(2));
        using var openVideo = new FileStream(Path.Combine(_root, "locked", "payload.bin"), FileMode.Open, FileAccess.Read, FileShare.None);

        var result = RunRetention.Prune([_root], new RetentionPolicy(KeepRuns: 1, MaxBytes: null), dryRun: false);

        Assert.Equal(["older"], result.Removed.Select(r => Path.GetFileName(r.Path)));
        Assert.Equal([Path.Combine(_root, "locked")], result.Failed);
    }

    private void WriteRun(string name, DateTime createdUtc, string? root = null)
    {
        var directory = Path.Combine(root ?? _root, name);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "payload.bin"), new byte[RunBytes]);
        Directory.SetCreationTimeUtc(directory, createdUtc);
    }

    private IEnumerable<string?> RemainingRuns() =>
        Directory.GetDirectories(_root).Select(Path.GetFileName).Order(StringComparer.Ordinal);
}
