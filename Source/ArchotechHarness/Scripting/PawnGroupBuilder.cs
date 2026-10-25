using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using ArchotechHarness.Layout;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ArchotechHarness.Scripting;

/// <summary>A group of one faction's pawns from Spawn.Group(faction): add pawns Of a kind, place them, then call Spawn().</summary>
public sealed class PawnGroupBuilder
{
    private const int EdgeInset = 3;
    private const int SpreadRadius = 4;

    private readonly PendingBuilders.Token _token;
    private readonly Faction _faction;
    private readonly List<(PawnKindDef Kind, int Count)> _members = new();
    private ThingDef _weapon;
    private IntVec3? _cell;
    private Rot4? _edge;
    private GroupOrders? _orders;

    internal PawnGroupBuilder(Faction faction, string caller)
    {
        _faction = faction;
        _token = ScriptSession.Current?.Builders.Track($"Spawn.Group() at {caller} ends with .Spawn()");
    }

    /// <summary>Adds count pawns of the kind; repeatable, such as a noble and their guards.</summary>
    public PawnGroupBuilder Of(PawnKindDef kind, int count = 1)
    {
        _members.Add((kind, count));
        return this;
    }

    /// <summary>Replaces every member's weapon with a new one of this def.</summary>
    public PawnGroupBuilder Armed(ThingDef weapon)
    {
        _weapon = weapon;
        return this;
    }

    /// <summary>Gathers the group around the cell, in place of FromEdge.</summary>
    public PawnGroupBuilder At(IntVec3 cell)
    {
        _cell = cell;
        return this;
    }

    /// <summary>Gathers the group at the middle of that map edge, on ground that reaches the colony.</summary>
    public PawnGroupBuilder FromEdge(Rot4 side)
    {
        _edge = side;
        return this;
    }

    /// <summary>Attacks the colony; the default for a faction hostile to the player.</summary>
    public PawnGroupBuilder Assaulting() => Ordered(GroupOrders.Assault);

    /// <summary>Visits the colony and leaves later; the default for any other faction.</summary>
    public PawnGroupBuilder Visiting() => Ordered(GroupOrders.Visit);

    /// <summary>Holds the ground where the group spawns.</summary>
    public PawnGroupBuilder Defending() => Ordered(GroupOrders.Defend);

    /// <summary>Spawns the group as one lord and returns its pawns.</summary>
    public IReadOnlyList<Pawn> Spawn()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        Validate();
        var map = World.Map;
        var orders = _orders ?? (_faction.HostileTo(Faction.OfPlayer) ? GroupOrders.Assault : GroupOrders.Visit);
        var start = _cell ?? EdgeStart(map, _edge!.Value);
        var pawns = new List<Pawn>();
        foreach (var (kind, count) in _members)
        {
            for (var i = 0; i < count; i++)
            {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, _faction,
                    mustBeCapableOfViolence: orders != GroupOrders.Visit, allowAddictions: false));
                if (_weapon != null)
                {
                    EquipCommand.Give(pawn, ThingFactory.Make(_weapon));
                }

                pawns.Add((Pawn)GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(start, map, SpreadRadius), map));
            }
        }

        LordMaker.MakeNewLord(_faction, LordJobFor(orders, start, pawns[0]), map, pawns);
        ScriptSession.Current?.Timeline.Add("group", $"{pawns.Count} {_faction.Name} pawns, {orders}");
        return pawns;
    }

    private PawnGroupBuilder Ordered(GroupOrders orders)
    {
        _orders = orders;
        return this;
    }

    private void Validate()
    {
        if (_faction == null)
        {
            throw new HarnessException("Spawn.Group needs a faction, such as Faction.OfPirates or World.Faction(def).");
        }

        if (_members.Count == 0 || _members.Any(m => m.Count < 1))
        {
            throw new HarnessException("Spawn.Group needs at least one Of(kind, count) with a count of 1 or more.");
        }

        if ((_cell == null) == (_edge == null))
        {
            throw new HarnessException("Spawn.Group needs exactly one of At(cell) or FromEdge(side).");
        }

        if (_weapon != null && _weapon.equipmentType != EquipmentType.Primary)
        {
            throw new HarnessException($"Spawn.Group().Armed({_weapon.defName}): {_weapon.defName} is not a weapon.");
        }
    }

    private LordJob LordJobFor(GroupOrders orders, IntVec3 start, Pawn first) => orders switch
    {
        GroupOrders.Assault => new LordJob_AssaultColony(_faction),
        GroupOrders.Defend => new LordJob_DefendPoint(start),
        _ => new LordJob_VisitColony(_faction, RCellFinder.TryFindRandomSpotJustOutsideColony(first, out var spot) ? spot : World.Center),
    };

    /// <summary>The standable cell along that edge nearest its middle that reaches the colony, else the middle itself.</summary>
    private static IntVec3 EdgeStart(Map map, Rot4 side)
    {
        var middle = MapEdge.Entry(side, map.Size, EdgeInset);
        var along = side.IsHorizontal ? IntVec3.North : IntVec3.East;
        var length = side.IsHorizontal ? map.Size.z : map.Size.x;
        return Enumerable.Range(0, length)
            .SelectMany(step => new[] { middle + along * step, middle - along * step })
            .Where(c => c.InBounds(map) && c.Standable(map))
            .FirstOrDefault(c => map.reachability.CanReachColony(c), middle);
    }
}
