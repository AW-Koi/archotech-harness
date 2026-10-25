namespace ArchotechHarness.Cli;

/// <summary>The run folders a prune removed, or would remove on a dry run, and those it could not delete.</summary>
public sealed record RetentionResult(IReadOnlyList<RunFolder> Removed, IReadOnlyList<string> Failed)
{
    public long FreedBytes => Removed.Sum(r => r.Bytes);
}
