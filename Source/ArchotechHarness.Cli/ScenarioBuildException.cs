namespace ArchotechHarness.Cli;

/// <summary>A scenarios project that does not build, with its compiler errors.</summary>
public sealed class ScenarioBuildException(string project, IReadOnlyList<string> errors)
    : Exception($"{project} does not build:\n" + string.Join('\n', errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
