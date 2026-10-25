namespace ArchotechHarness.Cli;

/// <summary>The installed mods a scenario requires, with their dependencies and the DLCs those need.</summary>
/// <remarks>SkipReason names the first mod that is not installed; the scenario is skipped when it is set.</remarks>
public sealed record RequiredModSet(IReadOnlyList<ModMetadata> Mods, IReadOnlyList<string> Dlcs, string? SkipReason)
{
    public static RequiredModSet Missing(string reason) => new([], [], reason);
}
