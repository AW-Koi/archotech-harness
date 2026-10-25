using System.Text.Json.Nodes;
using RimWorld;
using Verse;

namespace DevHarness.Commands;

/// <summary>Gives a pawn a new thing: weapons are equipped, apparel is worn, anything else goes to the inventory.</summary>
public static class EquipCommand
{
    public static CommandResult Run(JsonObject args)
    {
        var pawn = Arguments.Pawn(args);
        var thing = ThingFactory.Make(args);
        string slot;
        if (thing is Apparel apparel)
        {
            pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            slot = "apparel";
        }
        else if (thing.def.equipmentType == EquipmentType.Primary && thing is ThingWithComps equipment)
        {
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(equipment);
            slot = "equipment";
        }
        else
        {
            pawn.inventory.innerContainer.TryAdd(thing);
            slot = "inventory";
        }

        return CommandResult.Done(new JsonObject
        {
            ["pawn"] = pawn.LabelShort,
            ["id"] = thing.ThingID,
            ["def"] = thing.def.defName,
            ["slot"] = slot,
        });
    }
}
