using System.Text.Json.Nodes;
using DevHarness.Shared;

namespace DevHarness.Cli;

/// <summary>Checks a step result against its expectation.</summary>
public static class StepCheck
{
    /// <summary>Returns null when the expectation holds, otherwise a description of the mismatch.</summary>
    public static string? Failure(JsonNode? result, StepExpectation? expectation)
    {
        if (expectation == null)
        {
            return null;
        }

        var actual = JsonPath.Select(result, expectation.Path);
        if (JsonCondition.Holds(actual, expectation.Op, expectation.Value))
        {
            return null;
        }

        var subject = string.IsNullOrEmpty(expectation.Path) ? "result" : expectation.Path;
        var condition = expectation.Op == null ? "to be truthy" : $"{expectation.Op} {expectation.Value?.ToJsonString() ?? "null"}";
        return $"expected {subject} {condition}, got {actual?.ToJsonString() ?? "null"}";
    }
}
