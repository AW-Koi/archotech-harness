namespace Scenarios;

/// <summary>A tidy base from the presets: a furnished bedroom, a dining hall and a decorated kitchen on one power grid.</summary>
[Biome(nameof(BiomeDefOf.TemperateForest))]
public sealed class FurnishedColony : HarnessScenario
{
    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var yard = new CellRect(c.x - 16, c.z - 10, 36, 26);
        Build.Clearing(yard);
        World.Home(yard);

        // Three adjoining rooms on one hidden power grid: bedroom, dining hall, kitchen
        var bedroom = Build.Room()
            .Covering(new CellRect(c.x - 12, c.z, 8, 7))
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.East)
            .Powered()
            .Roofed()
            .Build();
        var dining = Build.Room()
            .Covering(new CellRect(bedroom.Bounds.maxX, c.z - 1, 12, 9))
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Adjoining()
            .Powered()
            .Roofed()
            .Build();
        var kitchenBounds = new CellRect(dining.Bounds.maxX, c.z - 1, 9, 9);
        var kitchen = Build.Room()
            .Covering(kitchenBounds)
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithFloor(TerrainDefOf.TileSandstone)
            .WithDoorAt(new IntVec3(kitchenBounds.minX, 0, kitchenBounds.CenterCell.z))
            .Adjoining()
            .Powered()
            .Roofed()
            .Build();

        var lover = Spawn.Colonist().WithName("Ada").Inside(dining).Doing(WorkTypeDefOf.Hauling).Spawn();
        var partner = Spawn.Colonist().WithName("Bram").At(new IntVec3(c.x, 0, c.z - 4)).Doing(WorkTypeDefOf.Hauling).Spawn();
        Colonists.RemoveAllExcept(lover, partner);

        // Presets: a bedroom, a dining hall, and work fittings before decorating the kitchen around them
        var furnishedBedroom = Furnish.Bedroom(bedroom)
            .Bed(ColonyDefOf.DoubleBed)
            .MadeOf(ThingDefOf.WoodLog)
            .Lamp()
            .Plants(ColonyDefOf.Plant_Rose)
            .Art(ColonyDefOf.SculptureSmall, ThingDefOf.BlocksGranite)
            .Owner(lover, partner)
            .Build();
        var furnishedDining = Furnish.Dining(dining, dining.Interior.ContractedBy(1))
            .MadeOf(ThingDefOf.WoodLog)
            .Build();
        var stove = Spawn.Thing(ColonyDefOf.FueledStove)
            .Facing(Rot4.South)
            .At(new IntVec3(kitchen.Interior.minX + 2, 0, kitchen.Interior.maxZ))
            .Fuelled()
            .WithBill(ColonyDefOf.CookMealSimple)
            .Spawn();
        var shelf = Spawn.Thing(ThingDefOf.Shelf)
            .MadeOf(ThingDefOf.WoodLog)
            .Facing(Rot4.West)
            .At(new IntVec3(kitchen.Interior.maxX, 0, kitchen.Interior.minZ + 1))
            .Holding(ThingDefOf.RawPotatoes, 60)
            .Spawn();
        var cooler = Build.InWall(ThingDefOf.Cooler, new IntVec3(kitchen.Interior.maxX - 1, 0, kitchen.Bounds.maxZ), Rot4.North);
        var decorated = Decorate.Room(kitchen)
            .Lamps()
            .PlantPots(ColonyDefOf.Plant_Rose)
            .Art(ColonyDefOf.SculptureSmall, ThingDefOf.BlocksGranite)
            .Build();

        // Power from a fuelled generator and a charged battery north of the kitchen
        var generatorCell = new IntVec3(kitchen.Center.x, 0, kitchen.Bounds.maxZ + 4);
        Spawn.Thing(ThingDefOf.WoodFiredGenerator).Facing(Rot4.North).At(generatorCell).Fuelled().Spawn();
        var battery = Spawn.Thing(ThingDefOf.Battery).Facing(Rot4.North).At(generatorCell + new IntVec3(3, 0, 0)).Charged().Spawn();
        Build.Conduit(generatorCell, new IntVec3(kitchen.Center.x, 0, kitchen.Bounds.maxZ));
        Build.Conduit(battery.Position, generatorCell);

        // Garden: a paved path from the dining door, trees, flowers and a tame cat
        Build.Path(new IntVec3(dining.Center.x, 0, dining.Bounds.minZ - 1), new IntVec3(dining.Center.x, 0, yard.minZ)).Of(TerrainDefOf.PavedTile).Wide(2).Build();
        var oak = Spawn.Plant(ThingDefOf.Plant_TreeOak).At(new IntVec3(yard.minX + 3, 0, yard.minZ + 4)).Spawn();
        Spawn.Plant(ThingDefOf.Plant_TreeOak).At(new IntVec3(yard.maxX - 3, 0, yard.minZ + 3)).Spawn();
        Spawn.Plant(ColonyDefOf.Plant_Rose).At(new IntVec3(dining.Center.x - 2, 0, dining.Bounds.minZ - 2)).Spawn();
        Spawn.Plant(ColonyDefOf.Plant_Rose).At(new IntVec3(dining.Center.x + 3, 0, dining.Bounds.minZ - 2)).Spawn();
        var cat = Spawn.Animal(ColonyDefOf.Cat).At(new IntVec3(dining.Center.x + 2, 0, dining.Bounds.minZ - 4)).Tame().WithName("Whiskers").Spawn();

        // Later commands win: a table over a shelf replaces it, and a wall drops the stockpile cell under it
        var yardShelf = Spawn.Thing(ThingDefOf.Shelf).Facing(Rot4.North).At(new IntVec3(yard.maxX - 4, 0, yard.minZ + 8)).Spawn();
        var yardTable = Spawn.Thing(ThingDefOf.Table2x2c).Facing(Rot4.North).At(yardShelf.Position).Spawn();
        var stockpileArea = new CellRect(yard.maxX - 6, yard.minZ + 2, 3, 3);
        var stockpile = Build.Stockpile(stockpileArea).Build();
        Spawn.Thing(ThingDefOf.Wall).MadeOf(ThingDefOf.BlocksGranite).Facing(Rot4.North).At(stockpileArea.CenterCell).Spawn();
        Check(yardShelf.Destroyed && yardTable.Spawned);
        Check(World.Map.zoneManager.ZoneAt(stockpileArea.CenterCell) == null && stockpile.Cells.Count == 8);

        // A one-cell closet: the sculpture takes the only free cell and the pot is skipped with a note, not an exception
        var closet = Build.Room()
            .Covering(new CellRect(bedroom.Bounds.minX, bedroom.Bounds.minZ - 3, 3, 4))
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithDoor(Rot4.South)
            .Adjoining()
            .Build();
        var closetDecor = Decorate.Room(closet)
            .PlantPots(ColonyDefOf.Plant_Rose)
            .Art(ColonyDefOf.SculptureSmall, ThingDefOf.BlocksGranite)
            .Build();
        Check(closetDecor.Things.Single().def == ColonyDefOf.SculptureSmall);

        // Nothing stands in a doorway
        HarnessRoom[] rooms = [bedroom, dining, kitchen, closet];
        var doorways = rooms.SelectMany(r => r.Bounds.EdgeCells.Where(e => e.GetDoor(World.Map) != null)
            .SelectMany(GenAdjFast.AdjacentCellsCardinal).Where(r.Interior.Contains)).ToList();
        Check(doorways.Count == 6);
        Check(doorways.All(d => !d.GetThingList(World.Map).Any(t => t.def.category == ThingCategory.Building && t.def.building?.isPowerConduit != true)));

        // The presets placed what they promise, facing the right way
        Check(furnishedBedroom.Bed.Rotation == Rot4.East);
        Check(furnishedBedroom.Bed.OwnersForReading.Count == 2);
        Check(furnishedBedroom.Things.Count == 8);
        Check(furnishedDining.Of(ThingDefOf.Table2x2c).Count == 2);
        Check(furnishedDining.Of(ThingDefOf.DiningChair).Count == 16);
        Check(decorated.Of(ThingDefOf.StandingLamp).Count >= 1);

        // Building blocks: fuel, charge, bills, storage, in-wall cooler, plants and the cat
        Check(stove.TryGetComp<CompRefuelable>().IsFull);
        Check(((IBillGiver)stove).BillStack.Bills.Single() is Bill_Production { repeatMode: var mode } && mode == BillRepeatModeDefOf.Forever);
        Check(battery.TryGetComp<CompPowerBattery>().StoredEnergyPct >= 0.999f);
        Check(((Building_Storage)shelf).AllSlotCellsList().Sum(s => s.GetThingList(World.Map).Where(t => t.def == ThingDefOf.RawPotatoes).Sum(t => t.stackCount)) == 60);
        Check(cooler.Position.GetEdifice(World.Map) == cooler);
        Check(oak.Growth >= 1f);
        Check(furnishedBedroom.Of(ColonyDefOf.PlantPot).All(pot => pot.Position.GetPlant(World.Map)?.def == ColonyDefOf.Plant_Rose));
        Check(cat.Faction == Faction.OfPlayer && cat.Name.ToStringShort == "Whiskers");

        // Game time switches the lamps and cooler on through the hidden conduits
        var consumers = World.Things(ThingDefOf.StandingLamp, yard).Append(cooler).Select(t => t.TryGetComp<CompPowerTrader>()).ToList();
        Check(consumers.Count >= 3);
        Clock.Speed = TimeSpeed.Normal;
        await Until(() => consumers.All(p => p.PowerOn), timeoutSeconds: 30);
        Clock.Speed = TimeSpeed.Paused;

        Clock.SkipToHour(10);
        Camera.Frame(yard);
        await Shot("colony");
        Camera.Look(dining.Center, zoom: 15f);
        await Shot("rooms");
    }
}
