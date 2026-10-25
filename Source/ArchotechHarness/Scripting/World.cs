using System;
using System.Collections.Generic;
using System.Linq;
using ArchotechHarness.Commands;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The current map and queries over it.</summary>
public static class World
{
    public static Map Map => Arguments.CurrentMap();

    public static IntVec3 Center => Map.Center;

    /// <summary>Filth on the map, optionally inside an area and only of the given filth ThingDefs.</summary>
    public static IReadOnlyList<Filth> Filth(CellRect? area = null, params ThingDef[] defs) =>
        Map.listerThings.ThingsInGroup(ThingRequestGroup.Filth)
            .OfType<Filth>()
            .Where(f => area == null || area.Value.Contains(f.Position))
            .Where(f => defs.Length == 0 || defs.Contains(f.def))
            .ToList();

    public static IReadOnlyList<Thing> Things(ThingDef def, CellRect? area = null) =>
        Map.listerThings.ThingsOfDef(def)
            .Where(t => area == null || area.Value.Contains(t.Position))
            .ToList();

    /// <summary>A spawned pawn by its short name (nickname) or ThingID.</summary>
    public static Pawn Pawn(string name) =>
        Map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.ThingID == name || p.LabelShort == name)
        ?? throw new HarnessException($"No spawned pawn called '{name}'.");

    /// <summary>Marks the area as home, where colonists clean, fight fires and repair.</summary>
    public static void Home(CellRect area)
    {
        foreach (var cell in area.ClipInsideMap(Map))
        {
            Map.areaManager.Home[cell] = true;
        }
    }

    /// <summary>Stops incidents, wake-ups, signal actions and letters for the rest of the script, ends active conditions and sets clear weather that holds.</summary>
    public static void Calm()
    {
        var session = ScriptSession.Current ?? throw new InvalidOperationException("No harness script is running.");
        session.Calm = true;
        session.Timeline.Add("world", "calm");
        foreach (var condition in Map.gameConditionManager.ActiveConditions.Concat(Find.World.gameConditionManager.ActiveConditions).ToList())
        {
            condition.suppressEndMessage = true;
            condition.End();
        }

        var weather = Map.weatherManager;
        weather.TransitionTo(WeatherDefOf.Clear);
        weather.TransitionTo(WeatherDefOf.Clear);
        weather.curWeatherAge = (int)WeatherManager.TransitionTicks;
    }

    /// <summary>Removes hostile and dormant pawns, strangers held in containers, and whatever wakes or spawns threats; returns how many.</summary>
    /// <remarks>That covers triggers, signal actions, mech cluster and hostile buildings, spawners such as hives, and wake-up comps.</remarks>
    public static int RemoveHostiles()
    {
        var threats = PawnSweep.Threats(Map).Concat(PawnSweep.ThreatSources(Map)).ToList();
        PawnSweep.Remove(threats);
        ScriptSession.Current?.Timeline.Add("world", $"removed {threats.Count} threats");
        return threats.Count;
    }
}
