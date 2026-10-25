using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Commands;

/// <summary>Removes spawned colonists other than those named in "keep", such as the quick-start crew.</summary>
public static class RemoveColonistsCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var map = Arguments.CurrentMap();
        var keep = Arguments.Strings(args, "keep").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = map.mapPawns.FreeColonistsSpawned
            .Where(p => !keep.Contains(p.LabelShort) && !keep.Contains(p.ThingID))
            .ToList();
        if (removed.Count == map.mapPawns.FreeColonistsSpawnedCount)
        {
            throw new HarnessException("Refusing to remove every colonist; name at least one in \"keep\".");
        }

        foreach (var pawn in removed)
        {
            pawn.Destroy();
        }

        return CommandResult.Done(new JsonObject
        {
            ["removed"] = new JsonArray(removed.Select(p => (JsonNode)p.LabelShort).ToArray()),
        });
    }
}
