namespace ArchotechHarness.Cli;

/// <summary>Deletes the oldest run folders under each root beyond the policy's count and size limits.</summary>
/// <remarks>Always keeps each root's newest run, and leaves loose files alone.</remarks>
public static class RunRetention
{
    public static RetentionResult Prune(IEnumerable<string> roots, RetentionPolicy policy, bool dryRun)
    {
        var removed = new List<RunFolder>();
        var failed = new List<string>();
        foreach (var run in roots.SelectMany(root => Expired(root, policy)))
        {
            if (dryRun || TryDelete(run.Path))
            {
                removed.Add(run);
            }
            else
            {
                failed.Add(run.Path);
            }
        }

        return new RetentionResult(removed, failed);
    }

    private static IEnumerable<RunFolder> Expired(string root, RetentionPolicy policy)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var newestFirst = Directory.GetDirectories(root).Select(RunFolder.Measure).OrderByDescending(r => r.CreatedUtc).ToList();
        var keep = Math.Max(1, policy.KeepRuns ?? int.MaxValue);
        long total = 0;
        for (var i = 0; i < newestFirst.Count; i++)
        {
            total += newestFirst[i].Bytes;
            var overCount = i >= keep;
            var overSize = i > 0 && total > policy.MaxBytes;
            if (overCount || overSize)
            {
                yield return newestFirst[i];
            }
        }
    }

    private static bool TryDelete(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
