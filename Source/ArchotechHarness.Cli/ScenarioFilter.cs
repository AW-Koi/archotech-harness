using System.Text.RegularExpressions;

namespace ArchotechHarness.Cli;

/// <summary>Matches scenario names against globs (* and ?), by simple or full class name, ignoring case.</summary>
/// <remarks>With no patterns, every scenario matches.</remarks>
public sealed class ScenarioFilter(IEnumerable<string> patterns)
{
    private readonly IReadOnlyList<Regex> _patterns = patterns
        .Select(p => new Regex("^" + Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase))
        .ToList();

    public bool Matches(string fullName)
    {
        if (_patterns.Count == 0)
        {
            return true;
        }

        var simpleName = fullName[(fullName.LastIndexOfAny(['.', '+']) + 1)..];
        return _patterns.Any(p => p.IsMatch(fullName) || p.IsMatch(simpleName));
    }
}
