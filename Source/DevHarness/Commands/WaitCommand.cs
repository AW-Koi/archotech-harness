using System;
using System.Text.Json.Nodes;
using DevHarness.Shared;
using Verse;

namespace DevHarness.Commands;

/// <summary>Waits for a number of ticks, an absolute tick, or a probe condition.</summary>
/// <remarks>The game must be unpaused for ticks to pass. Fails after "timeoutSeconds" of real time.</remarks>
public static class WaitCommand
{
    private const double DefaultTimeoutSeconds = 120;

    public static CommandResult Run(JsonObject args)
    {
        Arguments.CurrentMap();
        var started = DateTime.UtcNow;
        var deadline = started.AddSeconds(args["timeoutSeconds"]?.GetValue<double>() ?? DefaultTimeoutSeconds);
        var startTick = Find.TickManager.TicksGame;

        if (args["ticks"] != null || args["untilTick"] != null)
        {
            var target = args["untilTick"] != null ? Arguments.Int(args, "untilTick", 0) : startTick + Arguments.Int(args, "ticks", 0);
            return CommandResult.Pending(() =>
                Find.TickManager.TicksGame >= target
                    ? Outcome(started, null)
                    : CheckDeadline(deadline, $"tick {Find.TickManager.TicksGame} of {target}"));
        }

        var probe = Arguments.RequiredString(args, "probe");
        var probeArgs = ProbeCommand.ProbeArguments(args);
        var path = Arguments.String(args, "path");
        var op = Arguments.String(args, "op");
        var expected = args["value"]?.DeepClone();
        if (op == null && expected != null)
        {
            op = "eq";
        }

        return CommandResult.Pending(() =>
        {
            var value = JsonPath.Select(HarnessProbes.Run(probe, probeArgs), path);
            return JsonCondition.Holds(value, op, expected)
                ? Outcome(started, value)
                : CheckDeadline(deadline, $"{probe}{(path != null ? "." + path : "")} is {value?.ToJsonString() ?? "null"}");
        });
    }

    private static JsonNode Outcome(DateTime started, JsonNode value) => new JsonObject
    {
        ["tick"] = Find.TickManager.TicksGame,
        ["seconds"] = Math.Round((DateTime.UtcNow - started).TotalSeconds, 2),
        ["value"] = value?.DeepClone(),
    };

    private static JsonNode CheckDeadline(DateTime deadline, string progress) =>
        DateTime.UtcNow > deadline ? throw new HarnessException($"Wait timed out: {progress}.") : null;
}
