using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Reads a scenario JSON file and resolves its paths relative to that file.</summary>
public static class ScenarioLoader
{
    private static readonly string[] RunnerKeys = ["cmd", "save", "expect", "optional", "always", "comment"];

    public static Scenario Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var baseDirectory = Path.GetDirectoryName(fullPath)!;
        JsonObject root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(fullPath), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            }) as JsonObject ?? throw new ScenarioException($"{path}: the root must be a JSON object.");
        }
        catch (JsonException e)
        {
            throw new ScenarioException($"{path}: {e.Message}");
        }

        var window = root["window"] is JsonArray size && size.Count == 2
            ? new WindowSize(size[0]!.GetValue<int>(), size[1]!.GetValue<int>())
            : new WindowSize(1600, 900);

        return new Scenario(
            Name: root["name"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(fullPath),
            Dlcs: (root["dlcs"] as JsonArray)?.Select(d => d!.GetValue<string>()).ToList() ?? [],
            Mods: (root["mods"] as JsonArray)?.Select(m => ReadMod(m!.AsObject(), baseDirectory)).ToList() ?? [],
            Window: window,
            Steps: (root["steps"] as JsonArray)?.Select((s, i) => ReadStep(s as JsonObject, i, path)).ToList() ?? [],
            QuitWhenDone: root["quitWhenDone"]?.GetValue<bool>() ?? true);
    }

    private static ScenarioMod ReadMod(JsonObject mod, string baseDirectory)
    {
        var directory = mod["dir"]?.GetValue<string>() ?? throw new ScenarioException("Each mod needs a \"dir\".");
        BuildCommand? build = null;
        if (mod["build"] is JsonObject buildObject)
        {
            build = new BuildCommand(
                buildObject["command"]?.GetValue<string>() ?? throw new ScenarioException("A mod build needs a \"command\"."),
                Resolve(baseDirectory, buildObject["workingDir"]?.GetValue<string>() ?? directory));
        }

        return new ScenarioMod(Resolve(baseDirectory, directory), build);
    }

    private static ScenarioStep ReadStep(JsonObject? step, int index, string path)
    {
        var command = step?["cmd"]?.GetValue<string>()
            ?? throw new ScenarioException($"{path}: step {index + 1} has no \"cmd\".");
        var arguments = new JsonObject();
        foreach (var (key, value) in step)
        {
            if (!RunnerKeys.Contains(key))
            {
                arguments[key] = value?.DeepClone();
            }
        }

        StepExpectation? expectation = null;
        if (step["expect"] is JsonObject expect)
        {
            expectation = new StepExpectation(
                expect["path"]?.GetValue<string>(),
                expect["op"]?.GetValue<string>(),
                expect["value"]?.DeepClone());
        }

        return new ScenarioStep(
            command,
            arguments,
            step["save"]?.GetValue<string>(),
            expectation,
            step["optional"]?.GetValue<bool>() ?? false,
            step["always"]?.GetValue<bool>() ?? false);
    }

    private static string Resolve(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path)).TrimEnd(Path.DirectorySeparatorChar);
}
