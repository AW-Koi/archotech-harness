using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevHarness.Probes;

namespace DevHarness;

/// <summary>Public API for mods to expose in-game checks the harness can call by name.</summary>
/// <remarks>A probe takes its arguments as a JSON string and returns JSON text; it runs on the main thread.</remarks>
public static class HarnessProbes
{
    private static readonly ConcurrentDictionary<string, Func<JsonObject, JsonNode>> Registered =
        new(BuiltInProbes.All, StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, Func<string, string> probe) =>
        Registered[name] = args => ParseResult(probe(args.ToJsonString()));

    internal static void Register(string name, Func<JsonObject, JsonNode> probe) => Registered[name] = probe;

    internal static IEnumerable<string> Names => Registered.Keys.OrderBy(n => n);

    internal static JsonNode Run(string name, JsonObject args) =>
        Registered.TryGetValue(name, out var probe)
            ? probe(args)
            : throw new HarnessException($"No probe named '{name}'. Known: {string.Join(", ", Names)}.");

    internal static JsonNode ParseResult(string text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return JsonValue.Create(text);
        }
    }
}
