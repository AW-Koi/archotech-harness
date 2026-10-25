using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>A check on a step result: the value at Path compared with Value by Op.</summary>
public sealed record StepExpectation(string? Path, string? Op, JsonNode? Value);
