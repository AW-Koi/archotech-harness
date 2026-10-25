using Verse.AI.Group;

namespace Scenarios;

/// <summary>Drafted colonists hold a sandbag line while pirates advance from the north, a crewed mortar shells them and a raid arrives on cue.</summary>
public sealed class TrenchLine : HarnessScenario
{
    public override async Task Run()
    {
        // Open ground in a calm world, cleared for a battlefield
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var field = new CellRect(c.x - 15, c.z - 8, 31, 50);
        Build.Clearing(field);

        // A sandbag line with a strip of packed dirt behind it as a trench
        var sandbags = Build.Line(ThingDefOf.Sandbags, new IntVec3(c.x - 8, 0, c.z + 2), new IntVec3(c.x + 8, 0, c.z + 2)).Build();
        Build.Floor(new CellRect(c.x - 8, c.z + 1, 17, 1), TerrainDefOf.PackedDirt);

        // Four armed soldiers in flak vests hold the line, and a medic with medicine waits behind it
        var soldiers = Enumerable.Range(0, 4)
            .Select(i => Spawn.Colonist()
                .WithName($"Soldier{i + 1}")
                .At(new IntVec3(c.x - 6 + i * 4, 0, c.z + 1))
                .Armed(WarDefOf.Gun_AssaultRifle)
                .Wearing(WarDefOf.Apparel_FlakVest)
                .Drafted()
                .Spawn())
            .ToList();
        var medic = Spawn.Colonist()
            .WithName("Medic")
            .At(new IntVec3(c.x, 0, c.z - 3))
            .Doing(WorkTypeDefOf.Doctor)
            .WithSkill(SkillDefOf.Medicine, 12)
            .Spawn();
        Spawn.Thing(ThingDefOf.MedicineIndustrial).Stack(5).GivenTo(medic).Spawn();
        Colonists.RemoveAllExcept(soldiers.Append(medic).ToArray());
        medic.HoldAt(new IntVec3(c.x, 0, c.z));

        // Pirates in the open to the north, and a mortar behind them with its own crew
        var pirates = Spawn.Group(Faction.OfPirates)
            .Of(PawnKindDefOf.Pirate, 3)
            .Armed(WarDefOf.Gun_BoltActionRifle)
            .At(new IntVec3(c.x, 0, c.z + 28))
            .Assaulting()
            .Spawn();
        var mortar = Spawn.Turret(ThingDefOf.Turret_Mortar)
            .At(new IntVec3(c.x + 6, 0, c.z + 38))
            .OwnedBy(Faction.OfPirates)
            .Loaded(ThingDefOf.Shell_HighExplosive)
            .Manned(PawnKindDefOf.Pirate)
            .Spawn();

        // The line, the soldiers and the enemy are ready
        Check(sandbags.Count == 17);
        Check(sandbags.All(s => s.def == ThingDefOf.Sandbags && s.Faction == Faction.OfPlayer));
        Check(soldiers.All(s => s.Drafted && s.equipment.Primary?.def == WarDefOf.Gun_AssaultRifle));
        Check(soldiers.All(s => s.apparel.WornApparel.Any(a => a.def == WarDefOf.Apparel_FlakVest)));
        Check(pirates.Count == 3);
        Check(pirates.All(p => p.equipment.Primary?.def == WarDefOf.Gun_BoltActionRifle && p.GetLord()?.LordJob is LordJob_AssaultColony));
        Check(mortar.Faction == Faction.OfPirates);

        // Both lines by daylight
        Clock.SkipToHour(10);
        Camera.Frame(field);
        await Shot("lines");

        // Shells land short of the line, then walk closer
        var shortVolley = Spawn.Projectile(ThingDefOf.Shell_HighExplosive).At(new IntVec3(c.x, 0, c.z + 12)).FromEdge(Rot4.North).Scatter(3).Count(3).Spawn();
        Clock.Speed = TimeSpeed.Normal;
        await Until(() => shortVolley.All(p => p.Destroyed), timeoutSeconds: 30);
        var closeVolley = Spawn.Projectile(ThingDefOf.Shell_HighExplosive).At(new IntVec3(c.x, 0, c.z + 5)).FromEdge(Rot4.North).Scatter(2).Count(2).Spawn();
        await Until(() => closeVolley.All(p => p.Destroyed), timeoutSeconds: 30);

        // Every shell landed, the medic held their spot, and the enemy crew mans the mortar
        Check(shortVolley.Count == 3 && closeVolley.Count == 2);
        Check(medic.Position == new IntVec3(c.x, 0, c.z) && medic.Drafted);
        await Until(() => mortar.GetComp<CompMannable>().MannedNow, timeoutSeconds: 60);

        // A raid fires on cue despite the calm world
        var raiders = Spawn.Raid(800)
            .From(Faction.OfPirates)
            .Strategy(RaidStrategyDefOf.ImmediateAttack)
            .Arriving(PawnsArrivalModeDefOf.EdgeWalkIn)
            .Spawn();
        Note("raiders", raiders.Count);
        Check(raiders.Count > 0 && raiders.All(r => r.Faction == Faction.OfPirates && r.Spawned));

        // The firefight goes on until someone goes down
        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => World.Map.mapPawns.AllPawnsSpawned.Any(p => p.Faction == Faction.OfPirates && p.Downed) || soldiers.Any(s => s.Downed), timeoutSeconds: 90);
        Clock.Speed = TimeSpeed.Paused;
        await Shot("firefight");
    }
}
