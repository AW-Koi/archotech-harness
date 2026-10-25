using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Gives a pawn a new thing: weapons are equipped, apparel is worn, anything else goes to the inventory.</summary>
public static class EquipCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var pawn = Arguments.Pawn(args);
        var thing = ThingFactory.Make(args);
        var slot = Give(pawn, thing);
        return CommandResult.Done(new JsonObject
        {
            ["pawn"] = pawn.LabelShort,
            ["id"] = thing.ThingID,
            ["def"] = thing.def.defName,
            ["slot"] = slot,
        });
    }

    /// <summary>Returns the slot the thing went to: apparel, equipment or inventory.</summary>
    public static string Give(Pawn pawn, Thing thing)
    {
        if (thing is Apparel apparel)
        {
            pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            return "apparel";
        }

        if (thing.def.equipmentType == EquipmentType.Primary && thing is ThingWithComps equipment)
        {
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(equipment);
            return "equipment";
        }

        pawn.inventory.innerContainer.TryAdd(thing);
        return "inventory";
    }
}
