namespace Scenarios;

/// <summary>Two colonists marry at a marriage spot before an ex-lover, a parent, a best friend and a near stranger, then the colony parties.</summary>
public sealed class Wedding : HarnessScenario
{
    public override async Task Run()
    {
        // A calm world with a cleared yard marked as home
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var yard = new CellRect(c.x - 12, c.z - 10, 25, 22);
        Build.Clearing(yard);
        World.Home(yard);

        // A lit dining hall, a marriage spot in front of it and food for the guests
        var hall = Build.Room()
            .Covering(new CellRect(c.x - 6, c.z + 2, 13, 9))
            .WithFloor(TerrainDefOf.WoodPlankFloor)
            .WithDoor(Rot4.South)
            .Roofed()
            .Build();
        Furnish.Dining(hall, hall.Interior.ContractedBy(1)).MadeOf(ThingDefOf.WoodLog).Build();
        Decorate.Room(hall).Lamps().Build();
        var spot = new IntVec3(c.x, 0, c.z - 4);
        Spawn.Thing(ThingDefOf.MarriageSpot).Facing(Rot4.North).At(spot).Spawn();
        Spawn.Thing(ThingDefOf.MealSurvivalPack).Stack(30).At(new IntVec3(c.x + 8, 0, c.z - 8)).Spawn();

        // The couple and four guests gather at the spot
        var bride = Spawn.Colonist().WithName("Bride").At(spot + new IntVec3(-2, 0, -2)).Aged(27).Spawn();
        var groom = Spawn.Colonist().WithName("Groom").At(spot + new IntVec3(2, 0, -2)).Aged(29).Spawn();
        var ex = Spawn.Colonist().WithName("Ex").At(spot + new IntVec3(-4, 0, -4)).Spawn();
        var parent = Spawn.Colonist().WithName("Parent").At(spot + new IntVec3(-2, 0, -4)).Aged(60).Spawn();
        var friend = Spawn.Colonist().WithName("Friend").At(spot + new IntVec3(2, 0, -4)).Spawn();
        var stranger = Spawn.Colonist().WithName("Stranger").At(spot + new IntVec3(4, 0, -4)).Spawn();
        Colonists.RemoveAllExcept(bride, groom, ex, parent, friend, stranger);

        // The couple are lovers, the bride has an ex and a parent, the friend and groom are close, and the ex resents the groom
        bride.Relate(groom, PawnRelationDefOf.Lover);
        bride.Relate(ex, PawnRelationDefOf.ExLover);
        bride.Relate(parent, PawnRelationDefOf.Parent);
        friend.SetOpinionOf(groom, 80);
        groom.SetOpinionOf(friend, 80);
        ex.SetOpinionOf(groom, -35);

        // The wedding starts, turning the lovers into fiancés
        var ceremony = Ceremony.Wedding(bride, groom, spot);
        Check(bride.relations.DirectRelationExists(PawnRelationDefOf.Fiance, groom));
        Check(!bride.relations.DirectRelationExists(PawnRelationDefOf.Lover, groom));
        Check(ceremony.LordJob is LordJob_Joinable_MarriageCeremony);
        Check(stranger.relations.OpinionOf(bride) is > -20 and < 20);

        // The couple walk to the spot
        Clock.SkipToHour(14);
        Camera.Frame(yard);
        Clock.Speed = TimeSpeed.Normal;
        await Until(() => ceremony.ownedPawns.Count >= 2, timeoutSeconds: 60);
        await Shot("ceremony");

        // The ceremony ends with the pair married
        await FastForwardUntil(() => bride.relations.DirectRelationExists(PawnRelationDefOf.Spouse, groom), GenDate.TicksPerDay / 2);
        Check(groom.relations.DirectRelationExists(PawnRelationDefOf.Spouse, bride));

        // The best friend throws a party afterwards
        var party = Ceremony.Party(friend);
        Check(party.LordJob is LordJob_Joinable_Party);
        await FastForward(GenDate.TicksPerHour);
        Clock.Speed = TimeSpeed.Paused;
        await Shot("party");
    }
}
