using System.Linq;
using System.Text.Json.Nodes;

namespace DevHarness.Commands;

/// <summary>Runs a registered probe by "name" with optional "args", or lists the probes.</summary>
public static class ProbeCommand
{
    public static CommandResult Run(JsonObject args) =>
        CommandResult.Done(HarnessProbes.Run(Arguments.RequiredString(args, "name"), ProbeArguments(args)));

    public static CommandResult List(JsonObject args) =>
        CommandResult.Done(new JsonObject
        {
            ["probes"] = new JsonArray(HarnessProbes.Names.Select(n => (JsonNode)n).ToArray()),
            ["commands"] = new JsonArray(CommandRegistry.Names.Select(n => (JsonNode)n).ToArray()),
        });

    public static JsonObject ProbeArguments(JsonObject args) =>
        args["args"] is JsonObject probeArgs ? (JsonObject)probeArgs.DeepClone() : new JsonObject();
}
