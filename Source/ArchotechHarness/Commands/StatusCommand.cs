using System.Linq;
using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Reports the program state, active mods, tick, speed and loaded map.</summary>
public static class StatusCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var playing = Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null;
        var status = new JsonObject
        {
            ["programState"] = Current.ProgramState.ToString(),
            ["loading"] = LongEventHandler.AnyEventNowOrWaiting,
            ["mapLoaded"] = playing,
            ["devMode"] = Prefs.DevMode,
            ["version"] = VersionControl.CurrentVersionStringWithRev,
            ["mapGenerationError"] = MapGenerationFailure.Last?.ToString(),
            ["activeMods"] = new JsonArray(ModsConfig.ActiveModsInLoadOrder.Select(m => (JsonNode)m.PackageId).ToArray()),
        };
        if (playing)
        {
            var map = Find.CurrentMap;
            status["tick"] = Find.TickManager.TicksGame;
            status["speed"] = Find.TickManager.CurTimeSpeed.ToString();
            status["paused"] = Find.TickManager.Paused;
            status["playerHasControl"] = Current.Game.PlayerHasControl;
            status["mapSize"] = new JsonArray(map.Size.x, map.Size.z);
            status["colonists"] = new JsonArray(map.mapPawns.FreeColonistsSpawned
                .Select(p => (JsonNode)p.LabelShort).ToArray());
        }

        return CommandResult.Done(status);
    }
}
