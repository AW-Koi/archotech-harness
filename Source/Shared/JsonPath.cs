using System.Globalization;
using System.Text.Json.Nodes;

namespace DevHarness.Shared;

/// <summary>Selects a value by a dotted path such as "pawn.cell.0".</summary>
/// <remarks>Returns null when any segment is missing.</remarks>
public static class JsonPath
{
    public static JsonNode? Select(JsonNode? root, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return root;
        }

        var current = root;
        foreach (var segment in path.Split('.'))
        {
            current = current switch
            {
                JsonObject obj => obj.TryGetPropertyValue(segment, out var child) ? child : null,
                JsonArray array when int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    && index >= 0 && index < array.Count => array[index],
                _ => null,
            };
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }
}
