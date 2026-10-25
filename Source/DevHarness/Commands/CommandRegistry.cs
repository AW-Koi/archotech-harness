using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace DevHarness.Commands;

/// <summary>Maps command names to handlers that run on the main thread.</summary>
public static class CommandRegistry
{
    private static readonly Dictionary<string, Func<JsonObject, CommandResult>> Handlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = StatusCommand.Run,
            ["spawnPawn"] = SpawnPawnCommand.Run,
            ["spawnThing"] = SpawnThingCommand.Run,
            ["equip"] = EquipCommand.Run,
            ["buildRoom"] = BuildRoomCommand.Run,
            ["spawnFilth"] = SpawnFilthCommand.Run,
            ["setWork"] = SetWorkCommand.Run,
            ["removeColonists"] = RemoveColonistsCommand.Run,
            ["speed"] = SpeedCommand.Run,
            ["camera"] = CameraCommand.Run,
            ["wait"] = WaitCommand.Run,
            ["screenshot"] = ScreenshotCommand.Run,
            ["probe"] = ProbeCommand.Run,
            ["listProbes"] = ProbeCommand.List,
            ["log"] = LogCommand.Run,
            ["quit"] = QuitCommand.Run,
            ["runScenario"] = RunScenarioCommand.Run,
        };

    public static IEnumerable<string> Names => Handlers.Keys.OrderBy(n => n);

    public static CommandResult Run(string name, JsonObject arguments) =>
        Handlers.TryGetValue(name, out var handler)
            ? handler(arguments)
            : throw new HarnessException($"Unknown command '{name}'. Known: ping, {string.Join(", ", Names)}.");
}
