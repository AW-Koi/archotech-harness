namespace Scenarios;

/// <summary>A tribal raider held in a prison cell while a warden and a tribe-born colonist look after them, fast-forwarded two days.</summary>
public sealed class FeralPrisoner : HarnessScenario
{
    public override async Task Run()
    {
        // A calm world with a cleared yard marked as home
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var yard = new CellRect(c.x - 10, c.z - 8, 21, 17);
        Build.Clearing(yard);
        World.Home(yard);

        // A prison cell with a prisoner bed, and a pantry of meals
        var cell = Build.Room()
            .Covering(new CellRect(c.x - 3, c.z, 7, 6))
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Roofed()
            .Build();
        var prisonBed = (Building_Bed)Spawn.Thing(ThingDefOf.Bed).ForPrisoners().Facing(Rot4.South).At(new IntVec3(c.x, 0, cell.Interior.maxZ)).Spawn();
        var pantry = new CellRect(c.x + 5, c.z - 6, 3, 3);
        Build.Stockpile(pantry).Build();
        Spawn.Thing(ThingDefOf.MealSurvivalPack).Stack(20).At(pantry.CenterCell).Spawn();

        // A tribal raider asleep in the cell, a warden with high Social and a tribe-born colonist
        var tribe = World.Faction(FactionDefOf.TribeRough);
        var prisoner = Spawn.Prisoner().From(tribe).WithName("Feral").InBed(prisonBed).Spawn();
        var warden = Spawn.Colonist().WithName("Warden").At(new IntVec3(c.x, 0, c.z - 4)).Doing(WorkTypeDefOf.Warden).WithSkill(SkillDefOf.Social, 12).Spawn();
        var relay = Spawn.Colonist().WithName("Relay").At(new IntVec3(c.x - 2, 0, c.z - 4)).WithBackstory(IdentityDefOf.TribeChild19).KeepingOtherWork().Spawn();
        Colonists.RemoveAllExcept(warden, relay);

        // The raider is the colony's prisoner, in bed in a prison cell
        Check(prisoner.IsPrisonerOfColony && prisoner.Faction == tribe);
        Check(prisonBed.ForPrisoners);
        Check(prisonBed.GetRoom().IsPrisonCell);
        Check(prisoner.CurrentBed() == prisonBed && prisonBed.OwnersForReading.Contains(prisoner));
        Check(relay.story.Childhood == IdentityDefOf.TribeChild19);

        // The cell on the first morning
        Clock.SkipToHour(9);
        Camera.Frame(yard);
        await Shot("day1");

        // Two whole days pass, simulated as fast as the machine allows
        var startTick = Clock.Tick;
        var started = DateTime.UtcNow;
        await FastForward(2 * GenDate.TicksPerDay);
        Note("wallSecondsForTwoDays", Math.Round((DateTime.UtcNow - started).TotalSeconds, 1));

        // Exactly two days passed and the raider is still held, or joined
        Check(Clock.Tick - startTick == 2 * GenDate.TicksPerDay);
        Check(prisoner.IsPrisonerOfColony || prisoner.Faction == Faction.OfPlayer);

        // Fast-forwarding until a condition: the warden has talked to the prisoner by the end of day three
        await FastForwardUntil(() => warden.Memories().Count > 0 || prisoner.Memories().Any(m => m.otherPawn == warden), GenDate.TicksPerDay);
        Clock.Speed = TimeSpeed.Paused;
        await Shot("day3");
    }
}
