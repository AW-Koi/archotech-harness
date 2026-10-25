using System.Text.Json;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Writes every scenario run of an archo test session as one JSON document.</summary>
public static class RunReport
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static JsonObject ToJson(IReadOnlyList<ScenarioOutcome> outcomes, IReadOnlyList<GameLoad>? loads = null) => new()
    {
        ["scenarios"] = outcomes.Count,
        ["failed"] = outcomes.Count(o => !o.Passed),
        ["seconds"] = Math.Round(outcomes.Sum(o => o.SetupSeconds + o.RunSeconds), 2),
        ["runs"] = new JsonArray(outcomes.Select(o => (JsonNode)new JsonObject
        {
            ["scenario"] = o.Scenario.TypeName,
            ["file"] = o.Scenario.SourceFile,
            ["group"] = o.Group,
            ["passed"] = o.Passed,
            ["attempts"] = o.Attempts,
            ["setupSeconds"] = Math.Round(o.SetupSeconds, 2),
            ["runSeconds"] = Math.Round(o.RunSeconds, 2),
            ["error"] = o.Error,
            ["outputDirectory"] = o.OutputDirectory,
            ["report"] = o.Report?.DeepClone(),
        }).ToArray()),
        ["gameLoad"] = new JsonArray((loads ?? []).Select(l => (JsonNode)new JsonObject
        {
            ["group"] = l.Group,
            ["errors"] = new JsonArray(l.Errors.Select(e => (JsonNode)new JsonObject
            {
                ["text"] = e.Text,
                ["mod"] = e.Mod,
                ["modUnderTest"] = e.FromModUnderTest,
            }).ToArray()),
        }).ToArray()),
    };

    public static void WriteJson(string path, IReadOnlyList<ScenarioOutcome> outcomes, IReadOnlyList<GameLoad>? loads = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, ToJson(outcomes, loads).ToJsonString(Indented));
    }
}
