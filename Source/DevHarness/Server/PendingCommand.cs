using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DevHarness.Server;

/// <summary>A command waiting for the main thread, and the response the client awaits.</summary>
public sealed class PendingCommand(string name, JsonObject arguments, DateTime deadline)
{
    public string Name { get; } = name;

    public JsonObject Arguments { get; } = arguments;

    public DateTime Deadline { get; } = deadline;

    public Func<JsonNode> Poll { get; set; }

    public TaskCompletionSource<JsonObject> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Succeed(JsonNode result) =>
        Response.TrySetResult(new JsonObject { ["ok"] = true, ["result"] = result });

    public void Fail(string error, string kind) =>
        Response.TrySetResult(new JsonObject { ["ok"] = false, ["error"] = error, ["errorType"] = kind });
}
