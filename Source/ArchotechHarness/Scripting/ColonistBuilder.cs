using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A colonist to generate with Spawn.Colonist(); call Spawn() to put it on the map.</summary>
public sealed class ColonistBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly Dictionary<WorkTypeDef, int> _work = new();
    private readonly List<Trait> _traits = new();
    private readonly Dictionary<SkillDef, int> _skills = new();
    private string _name;
    private IntVec3? _cell;
    private HarnessRoom _room;
    private PawnKindDef _kind;
    private bool _onlyWork = true;
    private WorkTags _incapableOf;
    private ThingDef _weapon;
    private readonly List<ThingDef> _apparel = new();
    private bool _drafted;
    private float? _age;
    private XenotypeDef _xenotype;
    private readonly List<BackstoryDef> _backstories = new();

    internal ColonistBuilder(string caller) =>
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Colonist() at {caller} ends with .Spawn()");

    /// <summary>Sets the nickname, which World.Pawn finds it by.</summary>
    public ColonistBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Without At or Inside, the colonist stands at the map's centre.</summary>
    public ColonistBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>Places the colonist at the room's centre.</summary>
    public ColonistBuilder Inside(HarnessRoom room)
    {
        _room = room;
        return this;
    }

    /// <summary>Generates a colonist able to do this work, at priority 1.</summary>
    public ColonistBuilder Doing(params WorkTypeDef[] work)
    {
        foreach (var workType in work)
        {
            _work[workType] = 1;
        }

        return this;
    }

    /// <summary>Leaves other work enabled; by default the colonist does only the work given to Doing.</summary>
    public ColonistBuilder KeepingOtherWork()
    {
        _onlyWork = false;
        return this;
    }

    /// <summary>Swaps in a childhood that disables these work tags but none of the work given to Doing.</summary>
    public ColonistBuilder IncapableOf(WorkTags tags)
    {
        _incapableOf |= tags;
        return this;
    }

    /// <summary>Adds the trait, replacing traits it conflicts with. Spectrum traits such as Industriousness need a degree.</summary>
    public ColonistBuilder WithTrait(TraitDef trait, int degree = 0)
    {
        _traits.Add(new Trait(trait, degree));
        return this;
    }

    public ColonistBuilder WithSkill(SkillDef skill, int level)
    {
        _skills[skill] = level;
        return this;
    }

    /// <summary>Without it, PawnKindDefOf.Colonist.</summary>
    public ColonistBuilder OfKind(PawnKindDef kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>Replaces the colonist's weapon with a new one of this def.</summary>
    public ColonistBuilder Armed(ThingDef weapon)
    {
        _weapon = weapon;
        return this;
    }

    /// <summary>Puts on new apparel of this def, replacing what it covers; repeatable.</summary>
    public ColonistBuilder Wearing(ThingDef apparel)
    {
        _apparel.Add(apparel);
        return this;
    }

    /// <summary>Drafts the colonist where it spawns, so it holds that position and fires at will.</summary>
    public ColonistBuilder Drafted()
    {
        _drafted = true;
        return this;
    }

    /// <summary>The biological and chronological age in years; a child or baby needs Biotech.</summary>
    public ColonistBuilder Aged(float years)
    {
        _age = years;
        return this;
    }

    /// <summary>Needs Biotech.</summary>
    public ColonistBuilder Xenotype(XenotypeDef xenotype)
    {
        _xenotype = xenotype;
        return this;
    }

    /// <summary>Replaces the childhood or adulthood, by the backstory's slot; repeatable.</summary>
    public ColonistBuilder WithBackstory(BackstoryDef backstory)
    {
        _backstories.Add(backstory);
        return this;
    }

    public Pawn Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_cell != null && _room != null)
        {
            throw new HarnessException("A colonist goes At a cell or Inside a room, not both.");
        }

        if (_age is { } years && !(years >= 0))
        {
            throw new HarnessException($"Spawn.Colonist().Aged({years}) needs an age of 0 or more.");
        }

        if (_weapon != null && _weapon.equipmentType != EquipmentType.Primary)
        {
            throw new HarnessException($"Spawn.Colonist().Armed({_weapon.defName}): {_weapon.defName} is not a weapon.");
        }

        if (_apparel.FirstOrDefault(a => !a.IsApparel) is { } notApparel)
        {
            throw new HarnessException($"Spawn.Colonist().Wearing({notApparel.defName}): {notApparel.defName} is not apparel.");
        }

        var pawn = SpawnPawnCommand.Generate(World.Map, new ColonistPlan(_cell ?? _room?.Center ?? World.Center)
        {
            Nickname = _name,
            Kind = _kind ?? PawnKindDefOf.Colonist,
            Work = _work,
            OnlyWork = _onlyWork,
            Traits = _traits,
            Skills = _skills,
            IncapableOf = _incapableOf,
            Age = _age,
            Xenotype = _xenotype,
            Backstories = _backstories,
        });
        foreach (var gear in _apparel.Prepend(_weapon).Where(def => def != null))
        {
            EquipCommand.Give(pawn, ThingFactory.Make(gear));
        }

        if (_drafted)
        {
            pawn.drafter.Drafted = true;
        }

        return pawn;
    }
}
