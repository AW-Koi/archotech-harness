namespace Scenarios;

/// <summary>A small homestead in calm daylight: two adjoining rooms, furniture facing the right way, a stockpile, a potato field and a path.</summary>
public sealed class SmallColony : HarnessScenario
{
    public override async Task Run()
    {
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        Note("hostilesRemoved", World.RemoveHostiles());
        var c = World.Center;
        var yard = new CellRect(c.x - 13, c.z - 10, 30, 20);
        Build.Clearing(yard);

        var bedroom = Build.Room()
            .Covering(new CellRect(c.x - 10, c.z, 7, 6))
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.East)
            .WithDoor(Rot4.South)
            .Roofed()
            .Build();
        var sharedDoor = new IntVec3(bedroom.Bounds.maxX, 0, bedroom.Center.z);

        var diningRoom = Build.Room()
            .Covering(new CellRect(bedroom.Bounds.maxX, c.z, 9, 6))
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoorAt(new IntVec3(c.x, 0, c.z))
            .Adjoining()
            .Roofed()
            .Build();

        var bed = Spawn.Thing(ThingDefOf.Bed)
            .MadeOf(ThingDefOf.WoodLog)
            .Facing(Rot4.South)
            .At(new IntVec3(bedroom.Interior.minX + 1, 0, bedroom.Interior.maxZ))
            .Spawn();

        var table = Spawn.Thing(ThingDefOf.Table2x2c)
            .MadeOf(ThingDefOf.WoodLog)
            .Facing(Rot4.North)
            .At(new IntVec3(c.x, 0, c.z + 2))
            .Spawn();
        Spawn.Thing(ThingDefOf.DiningChair)
            .MadeOf(ThingDefOf.WoodLog)
            .Facing(Rot4.East)
            .At(new IntVec3(c.x - 1, 0, c.z + 2))
            .Spawn();
        Spawn.Thing(ThingDefOf.DiningChair)
            .MadeOf(ThingDefOf.WoodLog)
            .Facing(Rot4.West)
            .At(new IntVec3(c.x + 2, 0, c.z + 3))
            .Spawn();

        // A stockpile east of the house with some wood in it
        var stockpileArea = new CellRect(c.x + 8, c.z, 5, 5);
        var stockpile = Build.Stockpile(stockpileArea).Build();
        Spawn.Thing(ThingDefOf.WoodLog).Stack(75).At(stockpileArea.CenterCell).Spawn();

        // A potato field south-west, nearly ready to harvest
        var fieldArea = new CellRect(c.x - 10, c.z - 8, 9, 5);
        var field = Build.GrowingZone(fieldArea)
            .Planting(ThingDefOf.Plant_Potato)
            .Grown(0.8f)
            .Build();

        // A paved path from the dining room door south, and along to the stockpile
        Build.Floor(new CellRect(c.x, c.z - 8, 1, 8), TerrainDefOf.PavedTile);
        Build.Floor(new CellRect(c.x + 1, c.z - 2, 10, 1), TerrainDefOf.PavedTile);

        var farmer = Spawn.Colonist()
            .WithName("Farmer")
            .At(new IntVec3(fieldArea.maxX + 1, 0, fieldArea.CenterCell.z))
            .Doing(WorkTypeDefOf.Growing)
            .Spawn();
        var hauler = Spawn.Colonist()
            .WithName("Hauler")
            .Inside(diningRoom)
            .Doing(WorkTypeDefOf.Hauling)
            .Spawn();
        Colonists.RemoveAllExcept(farmer, hauler);

        var potatoes = World.Things(ThingDefOf.Plant_Potato, fieldArea).OfType<Plant>().ToList();
        Check(sharedDoor.GetDoor(World.Map) != null);
        Check(bed.Rotation == Rot4.South);
        Check(table.Rotation == Rot4.North);
        Check(potatoes.Count == fieldArea.Area);
        Check(potatoes.All(p => Math.Abs(p.Growth - 0.8f) < 0.001f));
        Check(World.Map.zoneManager.ZoneAt(stockpileArea.CenterCell) == stockpile);
        Check(World.Map.zoneManager.ZoneAt(fieldArea.CenterCell) == field);
        Check(new IntVec3(c.x, 0, c.z - 4).GetTerrain(World.Map) == TerrainDefOf.PavedTile);

        Clock.SkipToHour(9);
        Check(GenLocalDate.HourOfDay(World.Map) == 9);
        Camera.Frame(yard);
        await Shot("colony");

        // Three calm hours at superfast: no incidents, letters or hostiles, and the sky stays clear
        var raid = new FiringIncident(IncidentDefOf.RaidEnemy, null, StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, World.Map));
        Check(!Find.Storyteller.TryFire(raid));
        var letters = Find.LetterStack.LettersListForReading.Count;
        var lastIncident = Find.Storyteller.LastIncidentTick;
        Clock.Speed = TimeSpeed.Superfast;
        await Ticks(3 * GenDate.TicksPerHour);
        Clock.Speed = TimeSpeed.Paused;
        Check(Find.Storyteller.LastIncidentTick == lastIncident);
        Check(Find.LetterStack.LettersListForReading.Count == letters);
        Check(!World.Map.mapPawns.AllPawnsSpawned.Any(p => p.HostileTo(Faction.OfPlayer)));
        Check(World.Map.weatherManager.curWeather == WeatherDefOf.Clear);
        await Shot("midday");
    }
}
