using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArchotechHarness.Scripting;

/// <summary>Counts the errors the game logs, merging repeats and render-job lines that differ only in the job number.</summary>
public sealed class GameErrorTally
{
    private static readonly Regex JobNumber = new(@"\bJob \d+ faulted", RegexOptions.Compiled);

    private readonly object _lock = new();
    private readonly List<string> _order = new();
    private readonly Dictionary<string, (string Text, int Count)> _byKey = new();

    public IReadOnlyList<GameError> Distinct
    {
        get
        {
            lock (_lock)
            {
                return _order.Select(key => new GameError(_byKey[key].Text, _byKey[key].Count)).ToList();
            }
        }
    }

    public void Add(string text)
    {
        var key = JobNumber.Replace(text, "Job N faulted");
        lock (_lock)
        {
            if (_byKey.TryGetValue(key, out var seen))
            {
                _byKey[key] = (seen.Text, seen.Count + 1);
                return;
            }

            _order.Add(key);
            _byKey[key] = (text, 1);
        }
    }

    /// <summary>Null when the allow-list accepts every error.</summary>
    public string FailureMessage(GameErrorAllowList allowList)
    {
        var failing = Distinct.Where(e => !allowList.Allows(e.Text)).ToList();
        if (failing.Count == 0)
        {
            return null;
        }

        var first = failing[0].Text.Split('\n')[0].TrimEnd('\r');
        return $"GameErrors: the game logged {failing.Sum(e => e.Count)} errors ({failing.Count} distinct) during the scenario; the first: {first}";
    }
}
