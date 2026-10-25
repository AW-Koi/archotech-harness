using Verse.AI.Group;

namespace Scenarios;

/// <summary>An Empire count and two guards visit a colony of a knight, a glitterworld surgeon, a tribal hunter and a former pirate.</summary>
[Dlc("royalty")]
public sealed class ImperialVisit : HarnessScenario
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

        // A granite throne room with a throne against the north wall
        var throneRoom = Build.Room()
            .Covering(new CellRect(c.x - 5, c.z, 11, 9))
            .MadeOf(ThingDefOf.BlocksGranite)
            .WithFloor(TerrainDefOf.TileSandstone)
            .WithDoor(Rot4.South)
            .Roofed()
            .Build();
        var throne = Spawn.Thing(ThingDefOf.Throne).Facing(Rot4.South).At(new IntVec3(c.x, 0, throneRoom.Interior.maxZ)).Spawn();

        // A knight, a glitterworld surgeon, a tribal hunter and a former pirate
        var knight = Spawn.Colonist().WithName("Knight").Inside(throneRoom).Spawn();
        knight.GrantTitle(IdentityDefOf.Knight);
        var surgeon = Spawn.Colonist().WithName("Surgeon").At(new IntVec3(c.x - 3, 0, c.z - 4)).WithBackstory(IdentityDefOf.GlitterworldSurgeon15).Spawn();
        var hunter = Spawn.Colonist().WithName("Hunter").At(new IntVec3(c.x - 1, 0, c.z - 4))
            .WithBackstory(IdentityDefOf.TribeChild19)
            .WithBackstory(IdentityDefOf.Hunter73)
            .Spawn();
        var pirate = Spawn.Colonist().WithName("Pirate").At(new IntVec3(c.x + 1, 0, c.z - 4)).WithBackstory(IdentityDefOf.Pirate56).Spawn();
        Colonists.RemoveAllExcept(knight, surgeon, hunter, pirate);

        // A count and two guards arrive south-east of the throne room as one visiting lord
        var visitors = Spawn.Group(Faction.OfEmpire)
            .Of(IdentityDefOf.Empire_Royal_Count)
            .Of(PawnKindDefOf.Empire_Fighter_Trooper, 2)
            .At(new IntVec3(c.x + 9, 0, c.z - 6))
            .Visiting()
            .Spawn();
        var noble = visitors[0];

        // Each colonist has their title or backstory, and the visitors form one visiting lord around a titled noble
        Check(knight.royalty.GetCurrentTitle(Faction.OfEmpire) == IdentityDefOf.Knight);
        Check(surgeon.story.Adulthood == IdentityDefOf.GlitterworldSurgeon15);
        Check(hunter.story.Childhood == IdentityDefOf.TribeChild19 && hunter.story.Adulthood == IdentityDefOf.Hunter73);
        Check(pirate.story.Adulthood == IdentityDefOf.Pirate56);
        Check(visitors.Count == 3);
        Check(noble.royalty.MostSeniorTitle != null);
        Check(visitors.All(v => v.Faction == Faction.OfEmpire && v.GetLord() == noble.GetLord()));
        Check(noble.GetLord()?.LordJob is LordJob_VisitColony);
        Check(throne.Spawned);
        Note("nobleTitle", noble.royalty.MostSeniorTitle?.def.defName);

        // The arrival by daylight
        Clock.SkipToHour(10);
        Camera.Frame(yard);
        await Shot("arrival");

        // An hour later the visitors are still there and still friendly
        Clock.Speed = TimeSpeed.Superfast;
        await Ticks(GenDate.TicksPerHour);
        Clock.Speed = TimeSpeed.Paused;
        Check(visitors.All(v => v.Spawned && !v.HostileTo(Faction.OfPlayer)));
        await Shot("visit");
    }
}
