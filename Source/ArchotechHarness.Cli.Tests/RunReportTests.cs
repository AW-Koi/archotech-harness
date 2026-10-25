using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli.Tests;

public sealed class RunReportTests : IDisposable
{
    private const string PassedReport = """
        {"passed":true,"seconds":5.5,"checks":[{"check":"room.Filth().Count < 3","passed":true}],
         "notes":{"filthBefore":24},"shots":[],"gameErrors":[]}
        """;

    private const string FailedReport = """
        {"passed":false,"seconds":2.25,"error":"TimeoutException: Timed out waiting for room.Filth().Count == 0.",
         "at":["at MopsTheFloor.Run() in Mops.cs:line 12"],
         "checks":[{"check":"janitor.HasThought(\"Spotless\") && x < 1","passed":false}],
         "notes":{},"shots":[],"gameErrors":["Exception ticking Janitor"]}
        """;

    private static readonly WindowSize Window = new(1600, 900);

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Json_RunWithOneFailure_CountsScenariosAndKeepsEachReport()
    {
        var path = _temp.Combine("run.json");

        RunReport.WriteJson(path, [Outcome("MopsTheFloor", FailedReport), Outcome("SweepsTheHall", PassedReport)]);

        var run = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(2, run["scenarios"]!.GetValue<int>());
        Assert.Equal(1, run["failed"]!.GetValue<int>());
        var failed = run["runs"]!.AsArray().Single(r => r!["scenario"]!.GetValue<string>() == "MopMod.Scenarios.MopsTheFloor")!;
        Assert.Equal("TimeoutException: Timed out waiting for room.Filth().Count == 0.", failed["error"]!.GetValue<string>());
        Assert.Equal("Exception ticking Janitor", failed["report"]!["gameErrors"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Json_Run_RecordsGroupAttemptsAndTiming()
    {
        var path = _temp.Combine("run.json");

        RunReport.WriteJson(path, [Outcome("SweepsTheHall", PassedReport)]);

        var run = JsonNode.Parse(File.ReadAllText(path))!["runs"]![0]!;
        Assert.Equal("MopMod", run["group"]!.GetValue<string>());
        Assert.Equal(1, run["attempts"]!.GetValue<int>());
        Assert.Equal(1.25, run["setupSeconds"]!.GetValue<double>());
        Assert.Equal(5.5, run["runSeconds"]!.GetValue<double>());
    }

    [Fact]
    public void Html_FailedCheck_ShowsItsEscapedSourceText()
    {
        var path = _temp.Combine("report.html");

        HtmlReport.Write(path, [Outcome("MopsTheFloor", FailedReport)]);

        var html = File.ReadAllText(path);
        Assert.Contains("janitor.HasThought(&quot;Spotless&quot;) &amp;&amp; x &lt; 1", html);
        Assert.Contains("Exception ticking Janitor", html);
    }

    [Fact]
    public void Html_Screenshot_IsEmbeddedByItsPathRelativeToTheReport()
    {
        var shot = _temp.WriteFile("MopsTheFloor/before.png", "");
        var report = JsonNode.Parse(PassedReport)!.AsObject();
        report["shots"] = new JsonArray(shot);
        var path = _temp.Combine("report.html");

        HtmlReport.Write(path, [Outcome("MopsTheFloor", report.ToJsonString())]);

        Assert.Contains("<img src=\"MopsTheFloor/before.png\"", File.ReadAllText(path));
    }

    [Fact]
    public void Html_Timeline_ListsEachStepWithItsTimingAndLine()
    {
        var report = JsonNode.Parse(PassedReport)!.AsObject();
        report["timeline"] = JsonNode.Parse("""
            [{"at":0.1,"seconds":null,"tick":12,"kind":"build","text":"Build.Room() at Mops.cs:9","line":null,"passed":null},
             {"at":0.4,"seconds":3.25,"tick":2400,"kind":"until","text":"room.Filth().Count < before","line":21,"passed":true}]
            """);
        var path = _temp.Combine("report.html");

        HtmlReport.Write(path, [Outcome("MopsTheFloor", report.ToJsonString())]);

        var html = File.ReadAllText(path);
        Assert.Contains("<td>until</td><td><code>room.Filth().Count &lt; before</code></td><td>21</td><td>3.25 s</td>", html);
        Assert.True(html.IndexOf("Build.Room() at Mops.cs:9", StringComparison.Ordinal) < html.IndexOf("room.Filth().Count &lt; before", StringComparison.Ordinal));
    }

    [Fact]
    public void Html_Video_IsEmbeddedByItsPathRelativeToTheReport()
    {
        var video = _temp.WriteFile("MopsTheFloor/video.mp4", "");
        var report = JsonNode.Parse(PassedReport)!.AsObject();
        report["video"] = video;
        var path = _temp.Combine("report.html");

        HtmlReport.Write(path, [Outcome("MopsTheFloor", report.ToJsonString())]);

        Assert.Contains("<video controls src=\"MopsTheFloor/video.mp4\"", File.ReadAllText(path));
    }

    private ScenarioOutcome Outcome(string name, string report)
    {
        var json = JsonNode.Parse(report)!.AsObject();
        var scenario = new ScenarioClass($"MopMod.Scenarios.{name}", Path.Combine("Mods", "harness", "Mops.cs"),
            Path.Combine("Mods", "harness", "Scenarios.csproj"), [new ScenarioMod(Path.Combine("Mods", "MopMod", "mod"), null)], [], Window);
        return new ScenarioOutcome(scenario, "MopMod", json["passed"]!.GetValue<bool>(), 1, 1.25,
            json["seconds"]!.GetValue<double>(), json["error"]?.GetValue<string>(), json, _temp.Combine(name));
    }
}
