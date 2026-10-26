using System.Linq;
using System.Text.Json.Nodes;
using ArchotechHarness.Scripting;

namespace ArchotechHarness.Commands;

/// <summary>Returns every error the game logged from the harness's start until this command first ran.</summary>
public static class LoadErrorsCommand
{
    public static CommandResult Run(JsonObject args) => CommandResult.Done(new JsonObject
    {
        ["errors"] = new JsonArray(LoadErrorLog.Current.Close().Select(e => (JsonNode)e).ToArray()),
    });
}
