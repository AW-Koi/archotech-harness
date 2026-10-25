using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A room to build with Build.Room(): set its Size or Covering bounds, then call Build().</summary>
public sealed class RoomBuilder
{
    private readonly PendingBuilders.Token _token;
    private readonly List<Rot4> _doorSides = new();
    private readonly List<IntVec3> _doorCells = new();
    private CellRect? _covering;
    private IntVec3? _center;
    private int _width;
    private int _height;
    private TerrainDef _floor;
    private ThingDef _wall;
    private ThingDef _stuff;
    private bool _roof;
    private bool _adjoining;
    private bool _powered;

    internal RoomBuilder(string caller) =>
        _token = ScriptSession.Current?.Builders.Track($"Build.Room() at {caller} ends with .Build()");

    /// <summary>Without it, the room is centred on the map.</summary>
    public RoomBuilder CenteredOn(IntVec3 center)
    {
        _center = center;
        return this;
    }

    /// <summary>The bounds, walls included, in place of CenteredOn and Size; at least 3 by 3.</summary>
    public RoomBuilder Covering(CellRect bounds)
    {
        _covering = bounds;
        return this;
    }

    /// <summary>The outer size, walls included; at least 3 by 3.</summary>
    public RoomBuilder Size(int width, int height)
    {
        _width = width;
        _height = height;
        return this;
    }

    /// <summary>Without it, the ground inside stays as it was.</summary>
    public RoomBuilder WithFloor(TerrainDef floor)
    {
        _floor = floor;
        return this;
    }

    /// <summary>Without it, walls are ThingDefOf.Wall.</summary>
    public RoomBuilder WithWalls(ThingDef wall)
    {
        _wall = wall;
        return this;
    }

    /// <summary>The stuff for walls and door; without it, ThingDefOf.WoodLog.</summary>
    public RoomBuilder MadeOf(ThingDef stuff)
    {
        _stuff = stuff;
        return this;
    }

    /// <summary>Puts a door in the middle of that side's wall; repeatable. Without a door, the room has none.</summary>
    public RoomBuilder WithDoor(Rot4 side)
    {
        _doorSides.Add(side);
        return this;
    }

    /// <summary>Puts a door on this wall cell, which must not be a corner; repeatable.</summary>
    public RoomBuilder WithDoorAt(IntVec3 cell)
    {
        _doorCells.Add(cell);
        return this;
    }

    /// <summary>Clears only inside the bounds, so the room can share a wall with one built before it.</summary>
    public RoomBuilder Adjoining()
    {
        _adjoining = true;
        return this;
    }

    /// <summary>Hides power conduits in the walls, so powered things inside connect to them.</summary>
    public RoomBuilder Powered()
    {
        _powered = true;
        return this;
    }

    public RoomBuilder Roofed()
    {
        _roof = true;
        return this;
    }

    /// <summary>Clears the area, builds the finished room and removes any filth inside it.</summary>
    public HarnessRoom Build()
    {
        ScriptSession.Current?.Builders.Finish(_token);
        var bounds = Bounds();
        var doors = _doorSides.Select(side => DoorCell(bounds, side)).Concat(_doorCells).ToList();
        var misplaced = doors.Where(door => !bounds.IsOnEdge(door) || bounds.IsCorner(door)).ToList();
        if (misplaced.Count > 0)
        {
            throw new HarnessException($"Doors at {string.Join(", ", misplaced)} are not on a wall of the room covering {bounds}, or are on a corner.");
        }

        BuildRoomCommand.Build(World.Map, new RoomPlan(bounds)
        {
            Wall = _wall ?? ThingDefOf.Wall,
            Stuff = _stuff ?? ThingDefOf.WoodLog,
            Floor = _floor,
            Doors = doors,
            Roof = _roof,
            Adjoining = _adjoining,
            Powered = _powered,
        });
        return new HarnessRoom(bounds);
    }

    private CellRect Bounds()
    {
        if (_covering is { } covering)
        {
            if (_center != null || _width != 0 || _height != 0)
            {
                throw new HarnessException("A room takes Covering(bounds) or CenteredOn and Size, not both.");
            }

            RequireThreeByThree(covering.Width, covering.Height);
            return covering;
        }

        RequireThreeByThree(_width, _height);
        var center = _center ?? World.Center;
        var bounds = CellRect.CenteredOn(center, _width / 2, _height / 2);
        return new CellRect(bounds.minX, bounds.minZ, _width, _height);
    }

    private static void RequireThreeByThree(int width, int height)
    {
        if (width < 3 || height < 3)
        {
            throw new HarnessException($"A room needs a Size of at least 3 by 3, walls included; it has {width} by {height}.");
        }
    }

    private static IntVec3 DoorCell(CellRect bounds, Rot4 side) => side.AsInt switch
    {
        0 => new IntVec3(bounds.CenterCell.x, 0, bounds.maxZ),
        1 => new IntVec3(bounds.maxX, 0, bounds.CenterCell.z),
        2 => new IntVec3(bounds.CenterCell.x, 0, bounds.minZ),
        _ => new IntVec3(bounds.minX, 0, bounds.CenterCell.z),
    };
}
