using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Tests;

public sealed class ThreatSourceTests
{
    [Theory]
    [InlineData(typeof(RectTrigger))]
    [InlineData(typeof(SignalAction_Ambush))]
    [InlineData(typeof(SignalAction_DormancyWakeUp))]
    public void IsSource_ATriggerOrSignalAction_IsTrue(Type thingClass)
    {
        var def = new ThingDef { defName = "ArchoTest_Trigger", thingClass = thingClass };

        Assert.True(ThreatSource.IsSource(def));
    }

    [Theory]
    [InlineData(typeof(CompWakeUpDormant))]
    [InlineData(typeof(CompSpawnerPawn))]
    [InlineData(typeof(CompSpawnerHives))]
    [InlineData(typeof(CompSendSignalOnMotion))]
    [InlineData(typeof(CompSendSignalOnCountdown))]
    [InlineData(typeof(CompProximityFuse))]
    public void IsSource_ABuildingThatWakesOrSpawnsThreats_IsTrue(Type compClass)
    {
        var def = new ThingDef { defName = "ArchoTest_Cluster", thingClass = typeof(Building), comps = [new CompProperties { compClass = compClass }] };

        Assert.True(ThreatSource.IsSource(def));
    }

    [Fact]
    public void IsSource_AnOrdinaryBuilding_IsFalse()
    {
        var def = new ThingDef { defName = "ArchoTest_Wall", thingClass = typeof(Building), comps = [new CompProperties { compClass = typeof(CompForbiddable) }] };

        Assert.False(ThreatSource.IsSource(def));
    }
}
