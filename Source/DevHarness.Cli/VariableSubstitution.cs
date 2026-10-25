using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DevHarness.Shared;

namespace DevHarness.Cli;

/// <summary>Replaces ${name.path} references with values saved by earlier steps.</summary>
/// <remarks>A string that is only a reference takes the value's JSON type.</remarks>
public static partial class VariableSubstitution
{
    public static JsonNode? Apply(JsonNode? node, IReadOnlyDictionary<string, JsonNode?> variables)
    {
        switch (node)
        {
            case JsonObject obj:
                var copy = new JsonObject();
                foreach (var (key, value) in obj)
                {
                    copy[key] = Apply(value, variables);
                }

                return copy;
            case JsonArray array:
                return new JsonArray(array.Select(item => Apply(item, variables)).ToArray());
            case JsonValue value when value.TryGetValue(out string? text):
                return SubstituteText(text, variables);
            default:
                return node?.DeepClone();
        }
    }

    private static JsonNode? SubstituteText(string text, IReadOnlyDictionary<string, JsonNode?> variables)
    {
        var whole = Reference().Match(text);
        if (whole.Success && whole.Length == text.Length)
        {
            return Resolve(whole.Groups[1].Value, variables)?.DeepClone();
        }

        return JsonValue.Create(Reference().Replace(text, match =>
        {
            var resolved = Resolve(match.Groups[1].Value, variables);
            return resolved is JsonValue v && v.TryGetValue(out string? s) ? s : resolved?.ToJsonString() ?? "";
        }));
    }

    private static JsonNode? Resolve(string reference, IReadOnlyDictionary<string, JsonNode?> variables)
    {
        var dot = reference.IndexOf('.');
        var name = dot < 0 ? reference : reference[..dot];
        var path = dot < 0 ? null : reference[(dot + 1)..];
        if (!variables.TryGetValue(name, out var saved))
        {
            throw new ScenarioException($"Unknown variable '{name}' in '${{{reference}}}'. Save it with \"save\" on an earlier step.");
        }

        return JsonPath.Select(saved, path);
    }

    [GeneratedRegex(@"\$\{([A-Za-z0-9_.\-]+)\}")]
    private static partial Regex Reference();
}
