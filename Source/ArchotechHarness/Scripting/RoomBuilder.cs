using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>A room to build with Build.Room(): set its size, then call Build().</summary>
public sealed class RoomBuilder
{
    private readonly PendingBuilders.Token _token;
    private IntVec3? _center;
    private int _width;
    private int _height;
    private TerrainDef _floor;
    private ThingDef _wall;
    private ThingDef _stuff;
    private Rot4? _door;
    private bool _roof;

    internal RoomBuilder(string caller) =>
        _token = ScriptSession.Current?.Builders.Track($"Build.Room() at {caller} ends with .Build()");

    /// <summary>Without it, the room is centred on the map.</summary>
    public RoomBuilder CenteredOn(IntVec3 center)
    {
        _center = center;
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

    /// <summary>Puts a door in the middle of that side's wall. Without it, the room has no door.</summary>
    public RoomBuilder WithDoor(Rot4 side)
    {
        _door = side;
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
        if (_width < 3 || _height < 3)
        {
            throw new HarnessException($"A room needs a Size of at least 3 by 3, walls included; it has {_width} by {_height}.");
        }

        var center = _center ?? World.Center;
        var bounds = CellRect.CenteredOn(center, _width / 2, _height / 2);
        bounds = new CellRect(bounds.minX, bounds.minZ, _width, _height);
        BuildRoomCommand.Build(World.Map, new RoomPlan(bounds)
        {
            Wall = _wall ?? ThingDefOf.Wall,
            Stuff = _stuff ?? ThingDefOf.WoodLog,
            Floor = _floor,
            Door = _door is { } side ? DoorCell(bounds, side) : null,
            Roof = _roof,
        });
        return new HarnessRoom(bounds);
    }

    private static IntVec3 DoorCell(CellRect bounds, Rot4 side) => side.AsInt switch
    {
        0 => new IntVec3(bounds.CenterCell.x, 0, bounds.maxZ),
        1 => new IntVec3(bounds.maxX, 0, bounds.CenterCell.z),
        2 => new IntVec3(bounds.CenterCell.x, 0, bounds.minZ),
        _ => new IntVec3(bounds.minX, 0, bounds.CenterCell.z),
    };
}
