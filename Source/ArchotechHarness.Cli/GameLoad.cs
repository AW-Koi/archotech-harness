using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>The errors a game logged while it loaded, before its first scenario; Group labels the game in reports.</summary>
public sealed record GameLoad(string Group, IReadOnlyList<LoadError> Errors)
{
    private const int QuotedErrors = 3;

    private const string SecondDataLoadMarker = "LoadDefs called with already existing def packages";

    private const string OtherMods = "base game or another mod";

    /// <summary>The index in Errors of the first error from a second data load, after the first one failed; null without one.</summary>
    public int? SecondDataLoadStart { get; init; }

    public IReadOnlyList<LoadError> FromModsUnderTest => Errors.Where(e => e.FromModUnderTest).ToList();

    public int SecondDataLoadErrors => SecondDataLoadStart is { } start ? Errors.Count - start : 0;

    /// <summary>The mods under test that logged errors before the second data load.</summary>
    public IReadOnlyList<string> FirstDataLoadBrokenBy => SecondDataLoadStart is { } start
        ? Errors.Take(start).Where(e => e.FromModUnderTest).Select(e => e.Mod!).Distinct().ToList()
        : [];

    /// <summary>Why the first data load failed and what followed; null when the game loaded its data once.</summary>
    public string? SecondDataLoadExplanation => SecondDataLoadStart == null
        ? null
        : $"{(FirstDataLoadBrokenBy.Count > 0 ? string.Join(", ", FirstDataLoadBrokenBy) : "The base game or another mod")} broke the game's first data load, " +
          "so the game loaded its data a second time on top of it and added every def twice; " +
          $"{SecondDataLoadErrors} {(SecondDataLoadErrors == 1 ? "error comes" : "errors come")} from that second load.";

    /// <summary>Blames each logged error on a mod under test where it can, and finds where a second data load starts.</summary>
    public static GameLoad FromLog(string group, IReadOnlyList<string> loggedErrors, IReadOnlyList<ModFingerprint> modsUnderTest)
    {
        var errors = loggedErrors.Select(text => LoadErrorAttribution.Attribute(text, modsUnderTest)).ToList();
        var marker = errors.FindIndex(e => e.Text.StartsWith(SecondDataLoadMarker, StringComparison.Ordinal));
        return new GameLoad(group, errors) { SecondDataLoadStart = marker >= 0 ? marker : null };
    }

    /// <summary>Why the group's scenarios fail; null when no mod under test logged an error or errors are allowed.</summary>
    public string? Failure(bool allowLoadErrors)
    {
        var blocking = FromModsUnderTest;
        if (allowLoadErrors || blocking.Count == 0)
        {
            return null;
        }

        var shown = Repeats(blocking).Take(QuotedErrors).ToList();
        var quoted = string.Join(" | ", shown.Select(r => r.Quote())).TrimEnd('.');
        var unshown = blocking.Count - shown.Sum(r => r.Count);
        var more = unshown > 0 ? $" (and {unshown} more)" : "";
        var secondLoad = SecondDataLoadExplanation is { } explanation ? explanation + " " : "";
        return $"{secondLoad}The mods under test logged {blocking.Count} {(blocking.Count == 1 ? "error" : "errors")} while the game loaded: {quoted}{more}. " +
               "Pass --allow-load-errors to run the scenarios anyway.";
    }

    /// <summary>Console lines that count the errors and list the first few of each kind; empty without errors.</summary>
    public IReadOnlyList<string> Summary(int shownPerKind = 5)
    {
        if (Errors.Count == 0)
        {
            return [];
        }

        var fromModsUnderTest = FromModsUnderTest.Count;
        var lines = new List<string>
        {
            $"Game load for {Group}: {fromModsUnderTest} error(s) from mods under test, {Errors.Count - fromModsUnderTest} from the base game or other mods.",
        };
        if (SecondDataLoadExplanation is { } explanation)
        {
            lines.Add("  " + explanation);
        }

        foreach (var fromModUnderTest in new[] { true, false })
        {
            var errors = Errors.Where(e => e.FromModUnderTest == fromModUnderTest).ToList();
            var shown = Repeats(errors).Take(shownPerKind).ToList();
            lines.AddRange(shown.Select(r => $"  {(fromModUnderTest ? "Load error" : "Load warning")} ({r.Error.Mod ?? OtherMods}): {r.Quote()}"));
            var unshown = errors.Count - shown.Sum(r => r.Count);
            if (unshown > 0)
            {
                lines.Add($"  ...and {unshown} more; see the report.");
            }
        }

        return lines;
    }

    /// <summary>Every error with the mod it names, for run.json.</summary>
    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["group"] = Group,
            ["errors"] = new JsonArray(Errors.Select(e => (JsonNode)new JsonObject
            {
                ["text"] = e.Text,
                ["mod"] = e.Mod,
                ["modUnderTest"] = e.FromModUnderTest,
            }).ToArray()),
        };
        AddSecondDataLoad(json);
        return json;
    }

    /// <summary>The error count and the errors from mods under test, for a single run's report.</summary>
    public JsonObject ToCompactJson()
    {
        var json = new JsonObject
        {
            ["errors"] = Errors.Count,
            ["fromModsUnderTest"] = new JsonArray(Repeats(FromModsUnderTest).Select(r => (JsonNode)new JsonObject
            {
                ["text"] = r.Error.Text,
                ["mod"] = r.Error.Mod,
                ["count"] = r.Count,
            }).ToArray()),
        };
        AddSecondDataLoad(json);
        return json;
    }

    public static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

    private static IEnumerable<Repeat> Repeats(IEnumerable<LoadError> errors) =>
        errors.GroupBy(e => e.Text).Select(g => new Repeat(g.First(), g.Count()));

    private void AddSecondDataLoad(JsonObject json)
    {
        if (SecondDataLoadStart != null)
        {
            json["secondDataLoad"] = new JsonObject
            {
                ["brokenBy"] = new JsonArray(FirstDataLoadBrokenBy.Select(m => (JsonNode)m).ToArray()),
                ["errors"] = SecondDataLoadErrors,
            };
        }
    }

    /// <summary>One error text and how many times the game logged it.</summary>
    private sealed record Repeat(LoadError Error, int Count)
    {
        public string Quote() => Count > 1 ? $"{FirstLine(Error.Text)} ({Count} times)" : FirstLine(Error.Text);
    }
}
