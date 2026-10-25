namespace Scenarios;

/// <summary>A hospital after a raid: an amputee under anaesthetic, an infected arm, a patient bleeding out and one downed, with a corpse and blood outside.</summary>
public sealed class HospitalWard : HarnessScenario
{
    public override async Task Run()
    {
        // A calm world with a cleared yard marked as home
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var yard = new CellRect(c.x - 10, c.z - 10, 21, 20);
        Build.Clearing(yard);
        World.Home(yard);

        // A ward with four medical beds and a shelf of medicine
        var ward = Build.Room()
            .Covering(new CellRect(c.x - 6, c.z, 13, 7))
            .WithFloor(TerrainDefOf.TileSandstone)
            .WithDoor(Rot4.South)
            .Roofed()
            .Build();
        var beds = Enumerable.Range(0, 4)
            .Select(i => (Building_Bed)Spawn.Thing(ThingDefOf.Bed)
                .Medical()
                .Facing(Rot4.South)
                .At(new IntVec3(ward.Interior.minX + 1 + i * 3, 0, ward.Interior.maxZ))
                .Spawn())
            .ToList();
        Spawn.Thing(ThingDefOf.Shelf)
            .Facing(Rot4.North)
            .Holding(ThingDefOf.MedicineIndustrial, 20)
            .At(new IntVec3(ward.Interior.maxX - 1, 0, ward.Interior.minZ))
            .Spawn();

        // A doctor, four patients and one partner
        var doctor = Spawn.Colonist().WithName("Doctor").At(ward.Center + IntVec3.South).Doing(WorkTypeDefOf.Doctor).WithSkill(SkillDefOf.Medicine, 12).Spawn();
        var amputee = Spawn.Colonist().WithName("Amputee").Inside(ward).Spawn();
        var infected = Spawn.Colonist().WithName("Infected").Inside(ward).Spawn();
        var bleeder = Spawn.Colonist().WithName("Bleeder").Inside(ward).Spawn();
        var downed = Spawn.Colonist().WithName("Downed").Inside(ward).Spawn();
        var partner = Spawn.Colonist().WithName("Partner").At(new IntVec3(c.x, 0, c.z - 6)).Spawn();
        Colonists.RemoveAllExcept(doctor, amputee, infected, bleeder, downed, partner);

        // The wounds, then each patient into a medical bed
        amputee.RemovePart(BodyPartDefOf.Leg);
        amputee.AddHediff(HediffDefOf.Anesthetic, severity: 1f);
        infected.Wound(HediffDefOf.Cut, BodyPartDefOf.Arm, 6);
        infected.AddHediff(HediffDefOf.WoundInfection, BodyPartDefOf.Arm, 0.3f);
        bleeder.Wound(HospitalDefOf.Gunshot, BodyPartDefOf.Torso, 12);
        bleeder.Wound(HospitalDefOf.Gunshot, BodyPartDefOf.Lung, 6);
        downed.Down();
        foreach (var (patient, bed) in new[] { amputee, infected, bleeder, downed }.Zip(beds))
        {
            patient.PutInBed(bed);
        }

        // A dead raider outside the door and blood trailed in
        var raider = Spawn.Group(Faction.OfPirates).Of(PawnKindDefOf.Pirate).At(new IntVec3(c.x + 4, 0, c.z - 6)).Defending().Spawn()[0];
        raider.Kill(null);
        foreach (var z in Enumerable.Range(c.z - 6, 7))
        {
            Spawn.Filth(ThingDefOf.Filth_Blood, new IntVec3(c.x, 0, z));
        }

        // The partner breaks down at the sight
        Check(partner.StartMentalState(MentalStateDefOf.Wander_Sad));

        // Each patient lies in their bed with their wound, the raider is dead and the partner wanders in grief
        Check(beds.All(b => b.Medical));
        Check(amputee.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().Any(m => m.Part.def == BodyPartDefOf.Leg));
        Check(!amputee.health.capacities.CanBeAwake);
        Check(infected.health.hediffSet.HasHediff(HediffDefOf.WoundInfection));
        Check(bleeder.health.hediffSet.BleedRateTotal > 0);
        Check(downed.Downed);
        Check(new[] { amputee, infected, bleeder, downed }.Zip(beds).All(pair => pair.First.CurrentBed() == pair.Second));
        Check(raider.Dead && raider.Corpse is { Spawned: true });
        Check(partner.InMentalState && partner.MentalStateDef == MentalStateDefOf.Wander_Sad);

        // The ward by daylight
        Clock.SkipToHour(10);
        Camera.Frame(yard);
        await Shot("ward");

        // The doctor tends the patients; the bleeder dies anyway
        Clock.Speed = TimeSpeed.Superfast;
        await Until(() => bleeder.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any(i => i.IsTended()), timeoutSeconds: 120);
        bleeder.Kill(null);
        Clock.Speed = TimeSpeed.Paused;
        Check(bleeder.Dead);
        await Shot("tended");
    }
}
