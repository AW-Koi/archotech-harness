using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Creates colonists, things and filth on the current map.</summary>
public static class Spawn
{
    /// <summary>Starts a colonist; finish with Spawn().</summary>
    public static ColonistBuilder Colonist([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new($"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts a thing of this def; place it At a cell or give it to a pawn, then finish with Spawn().</summary>
    public static ThingBuilder Thing(ThingDef def, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(def, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Makes filth on the cell, or thickens filth already there; null when the cell takes no filth.</summary>
    public static Filth Filth(ThingDef def, IntVec3 at, int thickness = 1) =>
        SpawnFilthCommand.Spawn(World.Map, def, [at], thickness).FirstOrDefault();
}
