namespace Scenarios;

/// <summary>Fresh colonists build their first house from blueprints, hauling wood from a stockpile, until no blueprint or frame is left.</summary>
public sealed class NewHome : HarnessScenario
{
    public override async Task Run()
    {
        // A calm world with a cleared building site marked as home
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var site = new CellRect(c.x - 12, c.z - 10, 25, 20);
        Build.Clearing(site);
        World.Home(site);

        // Blueprints for a bedroom and a kitchen sharing a wall, a bed inside, and wood to build them with
        var bedroom = Build.Room()
            .Covering(new CellRect(c.x - 8, c.z, 7, 6))
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Roofed()
            .AsBlueprint()
            .Build();
        var kitchen = Build.Room()
            .Covering(new CellRect(bedroom.Bounds.maxX, c.z, 7, 6))
            .WithDoor(Rot4.South)
            .WithDoorAt(new IntVec3(bedroom.Bounds.maxX, 0, c.z + 2))
            .Adjoining()
            .Roofed()
            .AsBlueprint()
            .Build();
        var bed = Spawn.Thing(ThingDefOf.Bed).MadeOf(ThingDefOf.WoodLog).Facing(Rot4.South).At(new IntVec3(bedroom.Center.x, 0, bedroom.Interior.maxZ)).AsBlueprint().Spawn();
        var stockpile = new CellRect(c.x - 6, c.z - 7, 6, 4);
        Build.Stockpile(stockpile).Build();
        foreach (var cell in stockpile.Cells.Take(10))
        {
            Spawn.Thing(ThingDefOf.WoodLog).Stack(75).At(cell).Spawn();
        }

        // Meals so the builders don't go hungry
        Spawn.Thing(ThingDefOf.MealSurvivalPack).Stack(30).At(stockpile.Cells.Last()).Spawn();

        // Four skilled builders who also haul
        var builders = Enumerable.Range(1, 4)
            .Select(i => Spawn.Colonist()
                .WithName($"Builder{i}")
                .At(new IntVec3(c.x - 3 + i, 0, c.z - 3))
                .Doing(WorkTypeDefOf.Construction, WorkTypeDefOf.Hauling)
                .WithSkill(SkillDefOf.Construction, 10)
                .Spawn())
            .ToArray();
        Colonists.RemoveAllExcept(builders);

        // Only blueprints stand on the site, with the roof marked to build
        var house = new CellRect(bedroom.Bounds.minX, bedroom.Bounds.minZ, kitchen.Bounds.maxX - bedroom.Bounds.minX + 1, bedroom.Bounds.Height);
        var blueprints = World.Unbuilt(house).Count;
        Note("blueprints", blueprints);
        Check(bed is Blueprint_Build);
        Check(blueprints > 40);
        Check(World.Things(ThingDefOf.Wall, house).Count == 0);
        Check(World.Map.areaManager.BuildRoof[bedroom.Center]);

        // The site at dawn
        Clock.SkipToHour(7);
        Camera.Frame(site);
        await Shot("blueprints");

        // Days of building pass in seconds
        var startTick = Clock.Tick;
        await FastForwardUntil(() => World.Unbuilt(house).Count == 0 && house.ContractedBy(1).Cells.All(cell => World.Map.roofGrid.Roofed(cell) || cell.GetEdifice(World.Map) != null), 6 * GenDate.TicksPerDay);
        Clock.Speed = TimeSpeed.Paused;
        Note("daysToBuild", Math.Round(GenDate.TicksToDays(Clock.Tick - startTick), 2));

        // The floor, the bed and a proper indoor room stand where the blueprints were
        Check(bedroom.Interior.Cells.All(cell => cell.GetTerrain(World.Map) == TerrainDefOf.WoodPlankFloor));
        Check(World.Things(ThingDefOf.Bed, bedroom.Interior).Count == 1);
        Check(bedroom.Center.GetRoom(World.Map) is { PsychologicallyOutdoors: false, ProperRoom: true });
        await Shot("built");
    }
}
