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
    private Rot4? _facing;
    private bool _blueprint;
    private readonly ThingSetup _setup = new();

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

    /// <summary>Spawns a building exactly At its cell with this rotation; throws when the cell is blocked.</summary>
    public ThingBuilder Facing(Rot4 rot)
    {
        _facing = rot;
        return this;
    }

    /// <summary>Fills its fuel tank to capacity.</summary>
    public ThingBuilder Fuelled()
    {
        _setup.Fuelled = true;
        return this;
    }

    /// <summary>Fills its battery.</summary>
    public ThingBuilder Charged()
    {
        _setup.Charged = true;
        return this;
    }

    /// <summary>For a plant pot or other grower: sets the plant to grow and sows it fully grown.</summary>
    public ThingBuilder Growing(ThingDef plant)
    {
        _setup.Crop = plant;
        return this;
    }

    /// <summary>Makes a bed a medical bed.</summary>
    public ThingBuilder Medical()
    {
        _setup.Medical = true;
        return this;
    }

    /// <summary>Makes a bed a prisoner bed, so a room holding only prisoner beds is a prison cell.</summary>
    public ThingBuilder ForPrisoners()
    {
        _setup.ForPrisoners = true;
        return this;
    }

    /// <summary>Adds a do-forever bill to a work table; repeatable.</summary>
    public ThingBuilder WithBill(RecipeDef recipe)
    {
        _setup.Bills.Add(recipe);
        return this;
    }

    /// <summary>Puts stacks of the thing on a storage building's cells, such as a shelf; repeatable.</summary>
    public ThingBuilder Holding(ThingDef def, int count)
    {
        _setup.Stored.Add(new StoredStack(def, count));
        return this;
    }

    /// <summary>Places a blueprint of the building for colonists to build, in place of the finished building.</summary>
    public ThingBuilder AsBlueprint()
    {
        _blueprint = true;
        return this;
    }

    /// <summary>A weapon is equipped, apparel is worn and anything else goes to the pawn's inventory.</summary>
    public ThingBuilder GivenTo(Pawn pawn)
    {
        _pawn = pawn;
        return this;
    }

    /// <summary>Returns the blueprint when AsBlueprint is set.</summary>
    public Thing Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if ((_cell == null) == (_pawn == null))
        {
            throw new HarnessException($"Spawn.Thing({_def.defName}) needs exactly one of At(cell) or GivenTo(pawn).");
        }

        if (_facing != null && (_def.category != ThingCategory.Building || _cell == null))
        {
            throw new HarnessException($"Spawn.Thing({_def.defName}).Facing places a building At a cell; it is not a building, or has no cell.");
        }

        _setup.Validate(_def);
        if (_blueprint)
        {
            return PlaceBlueprint();
        }

        var thing = ThingFactory.Make(_def, _stuff, _quality);
        if (_cell is { } cell)
        {
            var spawned = _facing is { } rotation
                ? SpawnThingCommand.PlaceExactly(thing, cell, rotation, World.Map)
                : SpawnThingCommand.Place(thing, cell, World.Map, _count);
            _setup.Apply(spawned);
            return spawned;
        }

        thing.stackCount = System.Math.Min(_count, thing.def.stackLimit);
        EquipCommand.Give(_pawn, thing);
        return thing;
    }

    private Blueprint_Build PlaceBlueprint()
    {
        if (_def.category != ThingCategory.Building || _cell == null || !_setup.IsEmpty)
        {
            throw new HarnessException($"Spawn.Thing({_def.defName}).AsBlueprint places a building At a cell, with no starting state such as fuel or bills.");
        }

        return Blueprints.Place(World.Map, _def, _stuff, _cell.Value, _facing ?? Rot4.North);
    }
}
