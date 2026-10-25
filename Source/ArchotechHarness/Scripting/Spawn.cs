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

    /// <summary>Starts a plant in the ground, such as a tree or flower; place it At a cell, then finish with Spawn().</summary>
    public static PlantBuilder Plant(ThingDef plant, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(plant, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts an animal of this kind; place it At a cell, then finish with Spawn().</summary>
    public static AnimalBuilder Animal(PawnKindDef kind, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(kind, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts many colonists at once, such as for a scale test; finish with Spawn().</summary>
    public static CrowdBuilder Colonists(int count, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(count, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts a prisoner of the colony from another faction; place it, then finish with Spawn().</summary>
    public static PrisonerBuilder Prisoner([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new($"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts a group of one faction's pawns, such as raiders or visitors; add pawns Of a kind, place them, then finish with Spawn().</summary>
    public static PawnGroupBuilder Group(Faction faction, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(faction, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts a raid with these threat points, fired even while the world is calm; finish with Spawn().</summary>
    public static RaidBuilder Raid(float points, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(points, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts shells or other projectiles fired At a cell; finish with Spawn().</summary>
    public static ProjectileBuilder Projectile(ThingDef def, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(def, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Starts a turret or mortar; place it At a cell, then finish with Spawn().</summary>
    public static TurretBuilder Turret(ThingDef def, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) =>
        new(def, $"{Path.GetFileName(file)}:{line}");

    /// <summary>Makes filth on the cell, or thickens filth already there; null when the cell takes no filth.</summary>
    public static Filth Filth(ThingDef def, IntVec3 at, int thickness = 1) =>
        SpawnFilthCommand.Spawn(World.Map, def, [at], thickness).FirstOrDefault();
}
