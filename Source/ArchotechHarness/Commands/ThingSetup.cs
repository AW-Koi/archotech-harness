using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>The state a spawned thing starts in: fuel, charge, a crop, bed use, bills and stored stacks.</summary>
public sealed class ThingSetup
{
    public bool Fuelled { get; set; }

    public bool Charged { get; set; }

    public ThingDef Crop { get; set; }

    public bool Medical { get; set; }

    public bool ForPrisoners { get; set; }

    public List<RecipeDef> Bills { get; } = new();

    public List<StoredStack> Stored { get; } = new();

    /// <summary>Whether no option is set, as a blueprint requires.</summary>
    public bool IsEmpty => !Fuelled && !Charged && Crop == null && !Medical && !ForPrisoners && Bills.Count == 0 && Stored.Count == 0;

    /// <summary>Throws when the def can't take an option, before anything spawns.</summary>
    public void Validate(ThingDef def)
    {
        Require(!Fuelled || def.GetCompProperties<CompProperties_Refuelable>() != null, def, "Fuelled", "has no fuel tank");
        Require(!Charged || def.GetCompProperties<CompProperties_Battery>() != null, def, "Charged", "is not a battery");
        Require(Crop == null || Is<Building_PlantGrower>(def), def, "Growing", "is not a plant grower such as a plant pot");
        Require(!Medical || Is<Building_Bed>(def), def, "Medical", "is not a bed");
        Require(!ForPrisoners || Is<Building_Bed>(def), def, "ForPrisoners", "is not a bed");
        Require(Bills.Count == 0 || Is<IBillGiver>(def), def, "WithBill", "is not a work table");
        Require(Stored.Count == 0 || Is<Building_Storage>(def), def, "Holding", "is not storage such as a shelf");
    }

    /// <summary>Fills fuel and charge, sows the crop fully grown, adds do-forever bills and stocks the storage.</summary>
    public void Apply(Thing thing)
    {
        if (Fuelled && thing.TryGetComp<CompRefuelable>() is { } fuel)
        {
            fuel.Refuel(fuel.GetFuelCountToFullyRefuel());
        }

        if (Charged)
        {
            thing.TryGetComp<CompPowerBattery>()?.SetStoredEnergyPct(1f);
        }

        if (Crop != null && thing is Building_PlantGrower grower)
        {
            grower.SetPlantDefToGrow(Crop);
            foreach (var cell in grower.OccupiedRect())
            {
                PlantSower.Sow(Crop, cell, thing.Map, 1f);
            }
        }

        if (thing is Building_Bed bed)
        {
            bed.Medical = Medical || bed.Medical;
            if (ForPrisoners)
            {
                bed.ForPrisoners = true;
                bed.GetRoom()?.Notify_RoomShapeChanged();
            }
        }

        foreach (var recipe in Bills)
        {
            AddForeverBill((IBillGiver)thing, thing.def, recipe);
        }

        foreach (var stack in Stored)
        {
            Stock((Building_Storage)thing, stack);
        }
    }

    private static void AddForeverBill(IBillGiver table, ThingDef def, RecipeDef recipe)
    {
        if (!def.AllRecipes.Contains(recipe))
        {
            throw new HarnessException($"{def.defName} can't do the bill {recipe.defName}.");
        }

        var bill = recipe.MakeNewBill();
        if (bill is Bill_Production production)
        {
            production.repeatMode = BillRepeatModeDefOf.Forever;
        }

        table.BillStack.AddBill(bill);
    }

    private static void Stock(Building_Storage storage, StoredStack stack)
    {
        var map = storage.Map;
        var cells = storage.AllSlotCellsList();
        int Held() => cells.Sum(c => c.GetThingList(map).Where(t => t.def == stack.Def).Sum(t => t.stackCount));
        var target = Held() + stack.Count;
        while (Held() < target)
        {
            var before = Held();
            var item = ThingMaker.MakeThing(stack.Def, stack.Def.MadeFromStuff ? GenStuff.DefaultStuffFor(stack.Def) : null);
            item.stackCount = Math.Min(target - before, stack.Def.stackLimit);
            if (!cells.Any(c => GenPlace.TryPlaceThing(item, c, map, ThingPlaceMode.Direct)) && Held() == before)
            {
                throw new HarnessException($"{storage.def.defName} has no room for {target - before} more {stack.Def.defName}.");
            }
        }
    }

    private static bool Is<T>(ThingDef def) => def.thingClass != null && typeof(T).IsAssignableFrom(def.thingClass);

    private static void Require(bool ok, ThingDef def, string option, string problem)
    {
        if (!ok)
        {
            throw new HarnessException($"Spawn.Thing({def.defName}).{option}: {def.defName} {problem}.");
        }
    }
}
