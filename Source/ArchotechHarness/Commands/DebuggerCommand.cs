using System.Linq;
using System.Text.Json.Nodes;
using ArchotechHarness.Scripting;

namespace ArchotechHarness.Commands;

/// <summary>Reads or controls the running scenario: "action" is state (the default), pause, resume or step.</summary>
/// <remarks>The state lists the timeline steps from index "since" on, so a caller can poll for new ones.</remarks>
public static class DebuggerCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var session = ScriptSession.Current ?? throw new HarnessException("No scenario is running.");
        switch (Arguments.String(args, "action", "state"))
        {
            case "pause":
                session.PauseScript();
                break;
            case "resume":
                session.ResumeScript();
                break;
            case "step":
                session.StepScript();
                break;
            case "state":
                break;
            default:
                throw new HarnessException("The debugger action is state, pause, resume or step.");
        }

        var since = Arguments.Int(args, "since", 0);
        var steps = session.Timeline.Steps;
        return CommandResult.Done(new JsonObject
        {
            ["scenario"] = session.ScenarioName,
            ["paused"] = session.Control.Paused,
            ["stepping"] = session.Control.Stepping,
            ["waiting"] = new JsonArray(session.Waiting.Select(w => (JsonNode)w).ToArray()),
            ["stepCount"] = steps.Count,
            ["steps"] = new JsonArray(steps.Skip(since).Select(s => (JsonNode)new JsonObject
            {
                ["at"] = s.At,
                ["tick"] = s.Tick,
                ["kind"] = s.Kind,
                ["text"] = s.Text,
                ["line"] = s.Line == 0 ? null : s.Line,
                ["passed"] = s.Passed,
            }).ToArray()),
        });
    }
}
