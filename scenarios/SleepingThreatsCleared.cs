using Verse.AI.Group;

namespace Scenarios;

/// <summary>A planted ancient danger stays asleep under World.Calm, and World.RemoveHostiles leaves nothing to wake or spawn.</summary>
public sealed class SleepingThreatsCleared : HarnessScenario
{
    private const string WakeSignal = "ArchoSample.WakeTheMechs";

    public override async Task Run()
    {
        // A cleared patch of ground to plant an ancient danger in
        Clock.Speed = TimeSpeed.Paused;
        var c = World.Center;
        var danger = new CellRect(c.x + 4, c.z - 3, 7, 7);
        Build.Clearing(danger.ExpandedBy(6));

        // Sleeping mechanoids, a wake-up action, a floor trigger and a casket with a mech inside
        var lord = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenAssaultColony(Faction.OfMechanoids) { awakeOnClamor = true }, World.Map);
        foreach (var cell in new[] { danger.CenterCell, danger.CenterCell + IntVec3.East })
        {
            var mech = PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Scyther, Faction.OfMechanoids);
            GenSpawn.Spawn(mech, cell, World.Map);
            lord.AddPawn(mech);
        }
        var wakeUp = (SignalAction_DormancyWakeUp)ThingMaker.MakeThing(ThingDefOf.SignalAction_DormancyWakeUp);
        wakeUp.signalTag = WakeSignal;
        wakeUp.lord = lord;
        GenSpawn.Spawn(wakeUp, danger.CenterCell, World.Map);
        var trigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
        trigger.signalTag = WakeSignal;
        trigger.Rect = danger;
        GenSpawn.Spawn(trigger, danger.CenterCell, World.Map);
        var casket = (Building_Casket)GenSpawn.Spawn(ThingDefOf.AncientCryptosleepCasket, new IntVec3(danger.minX, 0, danger.minZ), World.Map, Rot4.North);
        casket.TryAcceptThing(PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Scyther, Faction.OfMechanoids));

        // Calm holds the wake-up signal back, and the cleanup takes the sleepers and everything that would wake them
        World.Calm();
        var letters = Find.LetterStack.LettersListForReading.Count;
        Find.SignalManager.SendSignal(new Signal(WakeSignal));
        Clock.Speed = TimeSpeed.Normal;
        await Ticks(60);
        Clock.Speed = TimeSpeed.Paused;
        Check(lord.CurLordToil is LordToil_Sleep);
        Note("threatsRemoved", World.RemoveHostiles());
        Check(!World.Map.mapPawns.AllPawns.Any(p => p.HostileTo(Faction.OfPlayer)));
        Check(World.Things(ThingDefOf.RectTrigger).Count == 0 && World.Things(ThingDefOf.SignalAction_DormancyWakeUp).Count == 0);
        Check(casket.GetDirectlyHeldThings().Count == 0);

        // A colonist walks through the old danger for three hours at superfast and nothing appears
        var walker = Spawn.Colonist().WithName("Walker").At(danger.CenterCell).KeepingOtherWork().Spawn();
        Colonists.RemoveAllExcept(walker);
        Clock.Speed = TimeSpeed.Superfast;
        await Ticks(3 * GenDate.TicksPerHour);
        Clock.Speed = TimeSpeed.Paused;
        Check(!World.Map.mapPawns.AllPawns.Any(p => p.HostileTo(Faction.OfPlayer)));
        Check(Find.LetterStack.LettersListForReading.Count == letters);
    }
}
