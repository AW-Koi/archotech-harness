namespace ArchotechHarness.Cli;

/// <summary>The errors one group's game logged while it loaded, before its first scenario.</summary>
public sealed record GameLoad(string Group, IReadOnlyList<LoadError> Errors)
{
    private const int QuotedErrors = 3;

    public IReadOnlyList<LoadError> FromModsUnderTest => Errors.Where(e => e.FromModUnderTest).ToList();

    /// <summary>Why the group's scenarios fail; null when no mod under test logged an error or errors are allowed.</summary>
    public string? Failure(bool allowLoadErrors)
    {
        var blocking = FromModsUnderTest;
        if (allowLoadErrors || blocking.Count == 0)
        {
            return null;
        }

        var quoted = string.Join(" | ", blocking.Take(QuotedErrors).Select(e => FirstLine(e.Text))).TrimEnd('.');
        var more = blocking.Count > QuotedErrors ? $" (and {blocking.Count - QuotedErrors} more)" : "";
        return $"The mods under test logged {blocking.Count} {(blocking.Count == 1 ? "error" : "errors")} while the game loaded: {quoted}{more}. " +
               "Pass --allow-load-errors to run the scenarios anyway.";
    }

    public static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');
}
