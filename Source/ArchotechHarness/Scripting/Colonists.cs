using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The colony's free colonists on the current map.</summary>
public static class Colonists
{
    public static IReadOnlyList<Pawn> All => World.Map.mapPawns.FreeColonistsSpawned.ToList();

    /// <summary>Removes every colonist not listed, such as the quick-start crew, and returns those removed.</summary>
    public static IReadOnlyList<Pawn> RemoveAllExcept(params Pawn[] keep) =>
        RemoveColonistsCommand.Remove(World.Map, keep.Contains);
}
