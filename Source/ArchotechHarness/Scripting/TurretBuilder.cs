using ArchotechHarness.Commands;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ArchotechHarness.Scripting;

/// <summary>A turret or mortar from Spawn.Turret(def): set At, optionally its owner, shells and crew, then call Spawn().</summary>
public sealed class TurretBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly ThingDef _def;
    private IntVec3? _cell;
    private Rot4 _facing = Rot4.North;
    private Faction _owner;
    private ThingDef _shell;
    private int _spares;
    private bool _manned;
    private PawnKindDef _crewKind;
    private Pawn _crew;

    internal TurretBuilder(ThingDef def, string caller)
    {
        _def = def;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Turret({def.defName}) at {caller} ends with .Spawn()");
    }

    /// <summary>Spawns it exactly on the cell, replacing what is in the way; required.</summary>
    public TurretBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>Without it, North.</summary>
    public TurretBuilder Facing(Rot4 rot)
    {
        _facing = rot;
        return this;
    }

    /// <summary>Without it, the player owns it.</summary>
    public TurretBuilder OwnedBy(Faction faction)
    {
        _owner = faction;
        return this;
    }

    /// <summary>Loads a shell, such as into a mortar, and piles spare shells beside it for the crew to reload.</summary>
    public TurretBuilder Loaded(ThingDef shell, int spares = 10)
    {
        _shell = shell;
        _spares = spares;
        return this;
    }

    /// <summary>Spawns a crew pawn of the owner's faction to man it; without a kind, the faction's basic member or a random humanlike kind.</summary>
    public TurretBuilder Manned(PawnKindDef crew = null)
    {
        _manned = true;
        _crewKind = crew;
        return this;
    }

    /// <summary>Sends this pawn to man it, in place of Manned.</summary>
    public TurretBuilder MannedBy(Pawn crew)
    {
        _crew = crew;
        return this;
    }

    /// <summary>Spawns it with its barrel and fuel full.</summary>
    public Building_TurretGun Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        Validate();
        var map = World.Map;
        var owner = _owner ?? Faction.OfPlayer;
        var turret = (Building_TurretGun)SpawnThingCommand.PlaceExactly(ThingFactory.Make(_def), _cell!.Value, _facing, map);
        turret.SetFaction(owner);
        if (turret.TryGetComp<CompRefuelable>() is { } fuel)
        {
            fuel.Refuel(fuel.GetFuelCountToFullyRefuel());
        }

        if (_shell != null)
        {
            turret.gun.TryGetComp<CompChangeableProjectile>()?.LoadShell(_shell, 1);
            if (_spares > 0)
            {
                SpawnThingCommand.Place(ThingMaker.MakeThing(_shell), turret.InteractionCell, map, _spares);
            }
        }

        var crew = _crew ?? (_manned ? GenerateCrew(owner, turret, map) : null);
        if (crew != null)
        {
            Man(turret, crew, map);
        }

        return turret;
    }

    private void Validate()
    {
        if (_def.thingClass == null || !typeof(Building_TurretGun).IsAssignableFrom(_def.thingClass))
        {
            throw new HarnessException($"Spawn.Turret({_def.defName}): {_def.defName} is not a turret.");
        }

        if (_shell != null && _shell.projectileWhenLoaded == null)
        {
            throw new HarnessException($"Spawn.Turret({_def.defName}).Loaded({_shell.defName}): {_shell.defName} is not a shell.");
        }

        if (_cell == null)
        {
            throw new HarnessException($"Spawn.Turret({_def.defName}) needs At(cell).");
        }

        if ((_manned || _crew != null) && _def.GetCompProperties<CompProperties_Mannable>() == null)
        {
            throw new HarnessException($"Spawn.Turret({_def.defName}).Manned: {_def.defName} takes no crew.");
        }
    }

    private Pawn GenerateCrew(Faction owner, Thing turret, Map map)
    {
        var kind = _crewKind ?? (owner == Faction.OfPlayer ? PawnKindDefOf.Colonist : owner.def.basicMemberKind ?? owner.RandomPawnKind());
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, owner, mustBeCapableOfViolence: true, allowAddictions: false));
        return (Pawn)GenSpawn.Spawn(pawn, turret.InteractionCell, map);
    }

    private static void Man(Building_TurretGun turret, Pawn crew, Map map)
    {
        if (crew.Faction == Faction.OfPlayer)
        {
            crew.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.ManTurret, turret), JobTag.Misc);
            return;
        }

        crew.GetLord()?.RemovePawn(crew);
        LordMaker.MakeNewLord(crew.Faction, new LordJob_ManTurrets(), map, [crew]);
    }
}
