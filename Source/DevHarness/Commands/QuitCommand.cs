using System.Text.Json.Nodes;
using DevHarness.Server;
using Verse;

namespace DevHarness.Commands;

/// <summary>Closes the game a few frames after replying, without saving.</summary>
public static class QuitCommand
{
    private const int FramesBeforeQuit = 10;

    public static CommandResult Run(JsonObject args)
    {
        MainThreadDispatcher.RunLater(FramesBeforeQuit, Root.Shutdown);
        return CommandResult.Done(new JsonObject { ["quitting"] = true });
    }
}
