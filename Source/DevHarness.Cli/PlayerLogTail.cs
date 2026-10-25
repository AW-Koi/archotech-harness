using System.Text.RegularExpressions;

namespace DevHarness.Cli;

/// <summary>Reads the last entries of a Waylith Player.log, optionally only warnings and errors.</summary>
public static partial class PlayerLogTail
{
    public static IReadOnlyList<string> Read(string path, int count, bool problemsOnly)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var entries = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (EntryStart().IsMatch(line) || entries.Count == 0)
            {
                entries.Add(line);
            }
            else if (!line.StartsWith("    at ", StringComparison.Ordinal))
            {
                entries[^1] += "\n" + line;
            }
        }

        var selected = problemsOnly ? entries.Where(e => ProblemLevel().IsMatch(e)).ToList() : entries;
        return selected.Skip(Math.Max(0, selected.Count - count)).ToList();
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}")]
    private static partial Regex EntryStart();

    [GeneratedRegex(@"^\S+ \S+\s+(WARN|ERROR|FATAL|CRIT)")]
    private static partial Regex ProblemLevel();
}
