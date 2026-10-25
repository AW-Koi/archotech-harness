using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>An animal from Spawn.Animal(kind): set At, then call Spawn().</summary>
public sealed class AnimalBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly PawnKindDef _kind;
    private IntVec3? _cell;
    private bool _tame;
    private string _name;

    internal AnimalBuilder(PawnKindDef kind, string caller)
    {
        _kind = kind;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Animal({kind.defName}) at {caller} ends with .Spawn()");
    }

    public AnimalBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>Makes it the player's; without it, the animal is wild.</summary>
    public AnimalBuilder Tame()
    {
        _tame = true;
        return this;
    }

    public AnimalBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public Pawn Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        if (_cell is not { } cell)
        {
            throw new HarnessException($"Spawn.Animal({_kind.defName}) needs At(cell).");
        }

        if (_kind.RaceProps is not { Animal: true })
        {
            throw new HarnessException($"Spawn.Animal({_kind.defName}): {_kind.defName} is not an animal.");
        }

        var animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(_kind, _tame ? Faction.OfPlayer : null));
        if (_name != null)
        {
            animal.Name = new NameSingle(_name);
        }

        return (Pawn)GenSpawn.Spawn(animal, cell, World.Map);
    }
}
