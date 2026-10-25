using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Scripting;

/// <summary>Creates colonists and things on the current map.</summary>
public static class Spawn
{
    /// <summary>Generates a colonist able to do the listed work; with onlyWork, every other work type is off.</summary>
    /// <remarks>Traits are "defName" or "defName:degree". The colonist stands in the room's centre unless "at" is given.</remarks>
    public static Pawn Colonist(string name, IntVec3? at = null, HarnessRoom inside = null, IEnumerable<WorkTypeDef> work = null,
        IEnumerable<string> traits = null, bool onlyWork = true, string kind = "Colonist")
    {
        var cell = at ?? inside?.Center ?? World.Center;
        var workObject = new JsonObject();
        foreach (var workType in work ?? Enumerable.Empty<WorkTypeDef>())
        {
            workObject[workType.defName] = 1;
        }

        var result = Script.CommandNow("spawnPawn", new JsonObject
        {
            ["name"] = name,
            ["cell"] = new JsonArray(cell.x, cell.z),
            ["kind"] = kind,
            ["work"] = workObject,
            ["onlyWork"] = onlyWork,
            ["traits"] = new JsonArray((traits ?? Enumerable.Empty<string>()).Select(t => (JsonNode)t).ToArray()),
        });
        return World.Pawn(result!["id"]!.GetValue<string>());
    }

    public static Thing Thing(string def, IntVec3 at, int count = 1, string stuff = null)
    {
        var arguments = new JsonObject { ["def"] = def, ["cell"] = new JsonArray(at.x, at.z), ["count"] = count };
        if (stuff != null)
        {
            arguments["stuff"] = stuff;
        }

        var id = Script.CommandNow("spawnThing", arguments)!["id"]!.GetValue<string>();
        return World.Map.listerThings.AllThings.First(t => t.ThingID == id);
    }

    public static bool Filth(string def, IntVec3 at, int thickness = 1) =>
        Script.CommandNow("spawnFilth", new JsonObject
        {
            ["def"] = def,
            ["cells"] = new JsonArray(new JsonArray(at.x, at.z)),
            ["thickness"] = thickness,
        })!["spawned"]!.GetValue<int>() == 1;
}
