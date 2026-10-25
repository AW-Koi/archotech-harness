using System;
using System.Linq;
using RimWorld;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Things that wake, spawn or call in threats: triggers, signal actions and spawner or wake-up comps.</summary>
public static class ThreatSource
{
    private static readonly Type[] SourceThings = [typeof(PawnTrigger), typeof(SignalAction)];

    private static readonly Type[] SourceComps =
    [
        typeof(CompWakeUpDormant), typeof(CompSpawnerPawn), typeof(CompSpawnerHives),
        typeof(CompSendSignalOnMotion), typeof(CompSendSignalOnCountdown), typeof(CompProximityFuse),
    ];

    public static bool IsSource(ThingDef def) =>
        def.thingClass != null && SourceThings.Any(t => t.IsAssignableFrom(def.thingClass))
        || def.comps.Any(c => c.compClass != null && SourceComps.Any(t => t.IsAssignableFrom(c.compClass)));
}
