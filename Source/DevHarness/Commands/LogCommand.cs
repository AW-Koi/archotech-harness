using System;
using System.Linq;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Commands;

/// <summary>Returns the tail of the in-game log, filtered by "minLevel" (message, warning or error).</summary>
public static class LogCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var minLevel = Enum.Parse<LogMessageType>(Arguments.String(args, "minLevel", "warning"), ignoreCase: true);
        var contains = Arguments.String(args, "contains");
        var messages = Log.Messages.ToList();
        var matching = messages
            .Where(m => m.type >= minLevel)
            .Where(m => contains == null || m.text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
        var tail = matching.Skip(Math.Max(0, matching.Count - Arguments.Int(args, "count", 50)));

        return CommandResult.Done(new JsonObject
        {
            ["errors"] = messages.Count(m => m.type == LogMessageType.Error),
            ["warnings"] = messages.Count(m => m.type == LogMessageType.Warning),
            ["entries"] = new JsonArray(tail.Select(m => (JsonNode)new JsonObject
            {
                ["type"] = m.type.ToString(),
                ["text"] = m.text,
                ["repeats"] = m.repeats,
            }).ToArray()),
        });
    }
}
