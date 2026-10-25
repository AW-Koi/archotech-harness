using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechHarness.Layout;

/// <summary>A room's floor for planning furniture: which interior cells are taken, kept clear, or free.</summary>
/// <remarks>Blocked cells hold something; reserved cells stay walkable, such as interaction cells and bed approaches.</remarks>
public sealed class RoomGrid
{
    private static readonly Rot4[] Sides = [Rot4.North, Rot4.East, Rot4.South, Rot4.West];

    private readonly HashSet<IntVec3> _taken;
    private readonly HashSet<IntVec3> _reserved;

    public RoomGrid(CellRect bounds, IEnumerable<IntVec3> doors, IEnumerable<IntVec3> blocked, IEnumerable<IntVec3> reserved)
    {
        Bounds = bounds;
        Interior = bounds.ContractedBy(1);
        var doorList = doors.ToList();
        DoorSide = doorList.Count == 0 ? Rot4.South : SideOf(doorList[0]);
        Doorways = doorList.Select(door => door + SideOf(door).Opposite.FacingCell).Where(Interior.Contains).ToList();
        _taken = blocked.ToHashSet();
        _reserved = reserved.Concat(Doorways).ToHashSet();
    }

    private RoomGrid(RoomGrid source)
    {
        Bounds = source.Bounds;
        Interior = source.Interior;
        DoorSide = source.DoorSide;
        Doorways = source.Doorways;
        _taken = source._taken.ToHashSet();
        _reserved = source._reserved.ToHashSet();
    }

    public CellRect Bounds { get; }

    public CellRect Interior { get; }

    /// <summary>The interior cell just inside each door.</summary>
    public IReadOnlyList<IntVec3> Doorways { get; }

    /// <summary>The wall the first door is on; South when the room has no door.</summary>
    public Rot4 DoorSide { get; }

    /// <summary>An independent copy, for trying a plan without changing this grid.</summary>
    public RoomGrid Copy() => new(this);

    public bool IsFree(IntVec3 cell) => Interior.Contains(cell) && !_taken.Contains(cell) && !_reserved.Contains(cell);

    public bool IsFree(CellRect rect) => rect.Cells.All(IsFree);

    public void Occupy(CellRect rect)
    {
        foreach (var cell in rect)
        {
            _taken.Add(cell);
        }
    }

    public void Reserve(IntVec3 cell) => _reserved.Add(cell);

    /// <summary>Free placements against a wall, each facing away from it into the room.</summary>
    public IEnumerable<Placement> WallSpots(IntVec2 size, bool rotatable)
    {
        foreach (var wall in Sides)
        {
            var rotation = rotatable ? wall.Opposite : Rot4.North;
            var footprint = Placement.Footprint(size, rotation);
            foreach (var rect in RectsAgainst(wall, footprint).Where(IsFree))
            {
                yield return Placement.Covering(rect, rotation, size);
            }
        }
    }

    /// <summary>Free wall spots that take up an interior corner.</summary>
    public IEnumerable<Placement> CornerSpots(IntVec2 size, bool rotatable) =>
        WallSpots(size, rotatable).Where(s => s.Rect.Cells.Any(c => Interior.IsCorner(c)));

    /// <summary>Free placements anywhere on the floor, in every rotation the thing allows.</summary>
    public IEnumerable<Placement> FloorSpots(IntVec2 size, bool rotatable)
    {
        foreach (var rotation in rotatable ? Sides : [Rot4.North])
        {
            var footprint = Placement.Footprint(size, rotation);
            for (var x = Interior.minX; x + footprint.x - 1 <= Interior.maxX; x++)
            {
                for (var z = Interior.minZ; z + footprint.z - 1 <= Interior.maxZ; z++)
                {
                    var rect = new CellRect(x, z, footprint.x, footprint.z);
                    if (IsFree(rect))
                    {
                        yield return Placement.Covering(rect, rotation, size);
                    }
                }
            }
        }
    }

    /// <summary>Free interior corners, from the south-west anticlockwise.</summary>
    public IReadOnlyList<IntVec3> FreeCorners() =>
        new[]
        {
            new IntVec3(Interior.minX, 0, Interior.minZ), new IntVec3(Interior.maxX, 0, Interior.minZ),
            new IntVec3(Interior.maxX, 0, Interior.maxZ), new IntVec3(Interior.minX, 0, Interior.maxZ),
        }.Where(IsFree).Distinct().ToList();

    /// <summary>Rects of this footprint lying along the wall, in order along it.</summary>
    public IEnumerable<CellRect> RectsAgainst(Rot4 wall, IntVec2 footprint)
    {
        if (footprint.x > Interior.Width || footprint.z > Interior.Height)
        {
            yield break;
        }

        if (wall.IsHorizontal)
        {
            var minX = wall == Rot4.East ? Interior.maxX - footprint.x + 1 : Interior.minX;
            for (var z = Interior.minZ; z + footprint.z - 1 <= Interior.maxZ; z++)
            {
                yield return new CellRect(minX, z, footprint.x, footprint.z);
            }
        }
        else
        {
            var minZ = wall == Rot4.North ? Interior.maxZ - footprint.z + 1 : Interior.minZ;
            for (var x = Interior.minX; x + footprint.x - 1 <= Interior.maxX; x++)
            {
                yield return new CellRect(x, minZ, footprint.x, footprint.z);
            }
        }
    }

    private Rot4 SideOf(IntVec3 door) =>
        door.z == Bounds.maxZ ? Rot4.North : door.x == Bounds.maxX ? Rot4.East : door.z == Bounds.minZ ? Rot4.South : Rot4.West;
}
