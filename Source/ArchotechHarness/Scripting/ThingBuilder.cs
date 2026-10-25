using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A thing to make with Spawn.Thing(def): place it At a cell or give it to a pawn, then call Spawn().</summary>
public sealed class ThingBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly ThingDef _def;
    private ThingDef _stuff;
    private QualityCategory? _quality;
    private int _count = 1;
    private IntVec3? _cell;
    private Pawn _pawn;

    internal ThingBuilder(ThingDef def, string caller)
    {
        _def = def;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Thing({def.defName}) at {caller} ends with .Spawn()");
    }

    /// <summary>Without it, a thing made from stuff uses the def's default stuff.</summary>
    public ThingBuilder MadeOf(ThingDef stuff)
    {
        _stuff = stuff;
        return this;
    }

    public ThingBuilder OfQuality(QualityCategory quality)
    {
        _quality = quality;
        return this;
    }

    /// <summary>The stack size, capped at the def's stack limit; 1 without it.</summary>
    public ThingBuilder Stack(int count)
    {
        _count = count;
        return this;
    }

    /// <summary>Places the thing on the cell, or the nearest free cell.</summary>
    public ThingBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>A weapon is equipped, apparel is worn and anything else goes to the pawn's inventory.</summary>
    public ThingBuilder GivenTo(Pawn pawn)
    {
        _pawn = pawn;
        return this;
    }

    public Thing Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if ((_cell == null) == (_pawn == null))
        {
            throw new HarnessException($"Spawn.Thing({_def.defName}) needs exactly one of At(cell) or GivenTo(pawn).");
        }

        var thing = ThingFactory.Make(_def, _stuff, _quality);
        if (_cell is { } cell)
        {
            return SpawnThingCommand.Place(thing, cell, World.Map, _count);
        }

        thing.stackCount = System.Math.Min(_count, thing.def.stackLimit);
        EquipCommand.Give(_pawn, thing);
        return thing;
    }
}
