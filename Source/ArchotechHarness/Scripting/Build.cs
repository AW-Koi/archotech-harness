using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>Builds finished structures instantly.</summary>
public static class Build
{
    /// <summary>Clears the area and builds a walled room centred on a cell; the size includes the walls.</summary>
    public static HarnessRoom Room(IntVec3 center, int width, int height, string floor = null, string wall = "Wall",
        string stuff = "WoodLog", Rot4? door = null, bool roof = false)
    {
        var bounds = CellRect.CenteredOn(center, width / 2, height / 2);
        bounds = new CellRect(bounds.minX, bounds.minZ, width, height);
        var doorSide = door ?? Rot4.South;
        var doorCell = doorSide.AsInt switch
        {
            0 => new IntVec3(bounds.CenterCell.x, 0, bounds.maxZ),
            1 => new IntVec3(bounds.maxX, 0, bounds.CenterCell.z),
            2 => new IntVec3(bounds.CenterCell.x, 0, bounds.minZ),
            _ => new IntVec3(bounds.minX, 0, bounds.CenterCell.z),
        };
        var arguments = new JsonObject
        {
            ["rect"] = new JsonArray(bounds.minX, bounds.minZ, width, height),
            ["wall"] = wall,
            ["stuff"] = stuff,
            ["door"] = new JsonArray(doorCell.x, doorCell.z),
            ["roof"] = roof,
        };
        if (floor != null)
        {
            arguments["floor"] = floor;
        }

        Script.CommandNow("buildRoom", arguments);
        return new HarnessRoom(bounds);
    }
}
