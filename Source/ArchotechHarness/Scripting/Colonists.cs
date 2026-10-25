using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The colony's free colonists on the current map.</summary>
public static class Colonists
{
    public static IReadOnlyList<Pawn> All => World.Map.mapPawns.FreeColonistsSpawned.ToList();

    /// <summary>Removes every colonist not listed, such as the quick-start crew.</summary>
    public static void RemoveAllExcept(params Pawn[] keep) =>
        Script.CommandNow("removeColonists", new JsonObject
        {
            ["keep"] = new JsonArray(keep.Select(p => (JsonNode)p.ThingID).ToArray()),
        });
}
