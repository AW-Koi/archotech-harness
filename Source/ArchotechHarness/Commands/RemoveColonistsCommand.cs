using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Removes spawned colonists other than those named in "keep", such as the quick-start crew.</summary>
public static class RemoveColonistsCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var keep = Arguments.Strings(args, "keep").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = Remove(Arguments.CurrentMap(), p => keep.Contains(p.LabelShort) || keep.Contains(p.ThingID));
        return CommandResult.Done(new JsonObject
        {
            ["removed"] = new JsonArray(removed.Select(p => (JsonNode)p.LabelShort).ToArray()),
        });
    }

    /// <summary>Destroys every free colonist the filter does not keep; refuses to remove them all.</summary>
    public static IReadOnlyList<Pawn> Remove(Map map, Func<Pawn, bool> keep)
    {
        var removed = map.mapPawns.FreeColonistsSpawned.Where(p => !keep(p)).ToList();
        if (removed.Count == map.mapPawns.FreeColonistsSpawnedCount)
        {
            throw new HarnessException("Refusing to remove every colonist; keep at least one.");
        }

        foreach (var pawn in removed)
        {
            pawn.Destroy();
        }

        return removed;
    }
}
