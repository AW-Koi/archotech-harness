using System;
using System.Text.Json.Nodes;

namespace DevHarness.Commands;

/// <summary>A finished command value, or a poll the dispatcher calls each frame until it returns non-null.</summary>
public sealed class CommandResult
{
    private CommandResult(JsonNode value, Func<JsonNode> poll)
    {
        Value = value;
        Poll = poll;
    }

    public JsonNode Value { get; }

    public Func<JsonNode> Poll { get; }

    public static CommandResult Done(JsonNode value) => new(value ?? new JsonObject(), null);

    public static CommandResult Pending(Func<JsonNode> poll) => new(null, poll);
}
