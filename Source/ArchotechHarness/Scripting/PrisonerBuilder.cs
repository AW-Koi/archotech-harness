using System.Collections.Generic;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A prisoner of the colony from Spawn.Prisoner(): place it At a cell, Inside a room or InBed, then call Spawn().</summary>
public sealed class PrisonerBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly List<Trait> _traits = new();
    private readonly Dictionary<SkillDef, int> _skills = new();
    private readonly List<BackstoryDef> _backstories = new();
    private Faction _faction;
    private bool _factionChosen;
    private PawnKindDef _kind;
    private string _name;
    private IntVec3? _cell;
    private HarnessRoom _room;
    private Building_Bed _bed;
    private float? _age;

    internal PrisonerBuilder(string caller) =>
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Prisoner() at {caller} ends with .Spawn()");

    /// <summary>The faction the prisoner came from; without it, Faction.OfPirates.</summary>
    public PrisonerBuilder From(Faction faction)
    {
        _faction = faction;
        _factionChosen = true;
        return this;
    }

    /// <summary>Without it, the faction's basic member, or one of its humanlike kinds at random.</summary>
    public PrisonerBuilder OfKind(PawnKindDef kind)
    {
        _kind = kind;
        return this;
    }

    public PrisonerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PrisonerBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>Places the prisoner at the room's centre, such as a prison cell.</summary>
    public PrisonerBuilder Inside(HarnessRoom room)
    {
        _room = room;
        return this;
    }

    /// <summary>Assigns the bed to the prisoner and puts them in it asleep, in place of At or Inside.</summary>
    public PrisonerBuilder InBed(Building_Bed bed)
    {
        _bed = bed;
        return this;
    }

    public PrisonerBuilder WithTrait(TraitDef trait, int degree = 0)
    {
        _traits.Add(new Trait(trait, degree));
        return this;
    }

    public PrisonerBuilder WithSkill(SkillDef skill, int level)
    {
        _skills[skill] = level;
        return this;
    }

    public PrisonerBuilder Aged(float years)
    {
        _age = years;
        return this;
    }

    /// <summary>Replaces the childhood or adulthood, by the backstory's slot; repeatable.</summary>
    public PrisonerBuilder WithBackstory(BackstoryDef backstory)
    {
        _backstories.Add(backstory);
        return this;
    }

    public Pawn Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_factionChosen && _faction == null)
        {
            throw new HarnessException("Spawn.Prisoner().From needs a faction, such as World.Faction(def).");
        }

        var places = (_cell == null ? 0 : 1) + (_room == null ? 0 : 1) + (_bed == null ? 0 : 1);
        if (places != 1)
        {
            throw new HarnessException("Spawn.Prisoner() needs exactly one of At(cell), Inside(room) or InBed(bed).");
        }

        if (_age is { } years && !(years >= 0))
        {
            throw new HarnessException($"Spawn.Prisoner().Aged({years}) needs an age of 0 or more.");
        }

        var faction = _faction ?? Faction.OfPirates;
        var pawn = SpawnPawnCommand.Generate(World.Map, new ColonistPlan(_cell ?? _room?.Center ?? _bed!.Position)
        {
            Nickname = _name,
            Kind = _kind ?? faction.def.basicMemberKind ?? faction.RandomPawnKind(),
            Faction = faction,
            Traits = _traits,
            Skills = _skills,
            Age = _age,
            Backstories = _backstories,
        });
        pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
        if (_bed != null)
        {
            _bed.CompAssignableToPawn.TryAssignPawn(pawn);
            pawn.PutInBed(_bed);
        }

        ScriptSession.Current?.Timeline.Add("prisoner", $"{pawn.LabelShort} of {faction.Name}");
        return pawn;
    }
}
