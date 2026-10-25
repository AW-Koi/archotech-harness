using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevHarness.Shared;

/// <summary>Compares a JSON value with eq, ne, lt, le, gt or ge.</summary>
/// <remarks>A null operator tests truthiness: true or a non-zero number.</remarks>
public static class JsonCondition
{
    public static bool Holds(JsonNode? actual, string? op, JsonNode? expected)
    {
        if (op == null)
        {
            return IsTruthy(actual);
        }

        var comparison = op.ToLowerInvariant();
        if (comparison is not ("eq" or "ne" or "lt" or "le" or "gt" or "ge"))
        {
            throw new JsonException($"Unknown operator '{op}'. Use eq, ne, lt, le, gt or ge.");
        }

        if (actual == null)
        {
            return comparison == "ne" && expected != null;
        }

        if (TryNumber(actual, out var a) && TryNumber(expected, out var b))
        {
            return comparison switch
            {
                "eq" => a.CompareTo(b) == 0,
                "ne" => a.CompareTo(b) != 0,
                "lt" => a < b,
                "le" => a <= b,
                "gt" => a > b,
                _ => a >= b,
            };
        }

        var equal = JsonNode.DeepEquals(actual, expected);
        return comparison switch
        {
            "eq" => equal,
            "ne" => !equal,
            _ => false,
        };
    }

    private static bool IsTruthy(JsonNode? node)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.True)
        {
            return true;
        }

        return TryNumber(node, out var number) && number != 0;
    }

    private static bool TryNumber(JsonNode? node, out double number)
    {
        number = 0;
        return node is JsonValue value
            && value.GetValueKind() == JsonValueKind.Number
            && double.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }
}
