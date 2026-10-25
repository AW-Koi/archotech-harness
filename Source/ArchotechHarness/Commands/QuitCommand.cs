using System.Text.Json.Nodes;
using ArchotechHarness.Server;
using Verse;

namespace ArchotechHarness.Commands;

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
