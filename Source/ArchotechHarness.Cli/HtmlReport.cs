using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Writes a single-page HTML report: per scenario its checks, notes, screenshots, game errors and timing.</summary>
/// <remarks>Screenshots are linked by paths relative to the report, so keep the run folder together.</remarks>
public static class HtmlReport
{
    private const string Style = """
        :root { color-scheme: light dark; --ok: #1f8a4c; --bad: #c0392b; --muted: #777; --line: #8884; }
        body { font: 14px/1.45 system-ui, sans-serif; margin: 0 auto; max-width: 1100px; padding: 16px; }
        h1 { font-size: 20px; } h2 { font-size: 16px; margin: 0; }
        .summary { margin-bottom: 16px; color: var(--muted); }
        details { border: 1px solid var(--line); border-radius: 8px; margin: 10px 0; padding: 10px 14px; }
        summary { cursor: pointer; display: flex; gap: 12px; align-items: baseline; flex-wrap: wrap; }
        .ok { color: var(--ok); } .bad { color: var(--bad); } .muted { color: var(--muted); }
        ul { padding-left: 20px; } code, pre { font: 12px ui-monospace, monospace; }
        pre { white-space: pre-wrap; overflow-wrap: anywhere; }
        img { max-width: 100%; border-radius: 4px; margin: 6px 0; }
        table { border-collapse: collapse; } td { border-top: 1px solid var(--line); padding: 2px 10px 2px 0; vertical-align: top; }
        """;

    public static void Write(string path, IReadOnlyList<ScenarioOutcome> outcomes)
    {
        var reportDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var html = new StringBuilder();
        var failed = outcomes.Count(o => !o.Passed);
        html.Append($"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Archotech Harness report</title><style>{Style}</style></head><body>");
        html.Append($"<h1>Archotech Harness report</h1><p class=\"summary\">{outcomes.Count} scenarios, <span class=\"{(failed == 0 ? "ok" : "bad")}\">{failed} failed</span>, {Seconds(outcomes.Sum(o => o.SetupSeconds + o.RunSeconds))} in game.</p>");
        foreach (var group in outcomes.GroupBy(o => o.Group))
        {
            html.Append($"<h2>{Encode(group.Key)}</h2>");
            foreach (var outcome in group)
            {
                AppendOutcome(html, outcome, reportDirectory);
            }
        }

        html.Append("</body></html>");
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(path, html.ToString());
    }

    private static void AppendOutcome(StringBuilder html, ScenarioOutcome outcome, string reportDirectory)
    {
        var report = outcome.Report;
        html.Append(outcome.Passed ? "<details>" : "<details open>");
        html.Append($"<summary><strong class=\"{(outcome.Passed ? "ok" : "bad")}\">{(outcome.Passed ? "Passed" : "Failed")}</strong>");
        html.Append($"<span>{Encode(outcome.Scenario.TypeName)}</span>");
        html.Append($"<span class=\"muted\">run {Seconds(outcome.RunSeconds)}, setup {Seconds(outcome.SetupSeconds)}");
        html.Append(outcome.Attempts > 1 ? $", {outcome.Attempts} attempts" : "");
        html.Append("</span></summary>");
        html.Append($"<p class=\"muted\"><code>{Encode(outcome.Scenario.SourceFile)}</code></p>");

        if (outcome.Error != null)
        {
            var at = report?["at"]?.AsArray().Select(a => a?.ToString()) ?? [];
            html.Append($"<pre class=\"bad\">{Encode(string.Join('\n', new[] { outcome.Error }.Concat(at)))}</pre>");
        }

        AppendList(html, "Checks", report?["checks"]?.AsArray().Select(c =>
            $"<span class=\"{(c?["passed"]?.GetValue<bool>() == true ? "ok\">&#10003;" : "bad\">&#10007;")}</span> <code>{Encode(c?["check"]?.ToString())}</code>"));
        if (report?["notes"] is JsonObject { Count: > 0 } notes)
        {
            html.Append("<h3>Notes</h3><table>");
            foreach (var (key, value) in notes)
            {
                html.Append($"<tr><td><code>{Encode(key)}</code></td><td><code>{Encode(value?.ToJsonString())}</code></td></tr>");
            }

            html.Append("</table>");
        }

        AppendTimeline(html, report?["timeline"] as JsonArray);
        AppendList(html, "Game errors", report?["gameErrors"]?.AsArray().Select(e => $"<pre class=\"bad\">{Encode(e?.ToString())}</pre>"));
        foreach (var shot in report?["shots"]?.AsArray().Select(s => s?.ToString()).OfType<string>() ?? [])
        {
            var relative = Path.GetRelativePath(reportDirectory, shot).Replace('\\', '/');
            html.Append($"<figure><img src=\"{Encode(relative)}\" alt=\"{Encode(Path.GetFileName(shot))}\" loading=\"lazy\"><figcaption class=\"muted\">{Encode(Path.GetFileName(shot))}</figcaption></figure>");
        }

        if (report?["video"]?.ToString() is { } video)
        {
            var relative = Path.GetRelativePath(reportDirectory, video).Replace('\\', '/');
            html.Append($"<video controls src=\"{Encode(relative)}\" style=\"max-width:100%\"></video>");
        }
        else if (report?["videoError"]?.ToString() is { } videoError)
        {
            html.Append($"<p class=\"muted\">No video: {Encode(videoError)}</p>");
        }

        html.Append("</details>");
    }

    private static void AppendTimeline(StringBuilder html, JsonArray? steps)
    {
        if (steps is not { Count: > 0 })
        {
            return;
        }

        html.Append("<h3>Timeline</h3><table><tr><td>At</td><td>Tick</td><td>Step</td><td>What</td><td>Line</td><td>Took</td></tr>");
        foreach (var step in steps.OfType<JsonObject>())
        {
            var failed = step["passed"]?.GetValue<bool>() == false;
            html.Append(failed ? "<tr class=\"bad\">" : "<tr>");
            html.Append($"<td>{Seconds(step["at"]?.GetValue<double>() ?? 0)}</td><td>{step["tick"]}</td>");
            html.Append($"<td>{Encode(step["kind"]?.ToString())}</td><td><code>{Encode(step["text"]?.ToString())}</code></td>");
            html.Append($"<td>{step["line"]}</td><td>{(step["seconds"] is JsonValue took ? Seconds(took.GetValue<double>()) : "")}</td></tr>");
        }

        html.Append("</table>");
    }

    private static void AppendList(StringBuilder html, string title, IEnumerable<string>? items)
    {
        var list = items?.ToList() ?? [];
        if (list.Count == 0)
        {
            return;
        }

        html.Append($"<h3>{title}</h3><ul>");
        foreach (var item in list)
        {
            html.Append($"<li>{item}</li>");
        }

        html.Append("</ul>");
    }

    private static string Encode(string? text) => WebUtility.HtmlEncode(text ?? "");

    private static string Seconds(double seconds) => Math.Round(seconds, 2).ToString(CultureInfo.InvariantCulture) + " s";
}
