using HarmonyLib;
using RimWorld;

namespace ArchotechHarness.Scripting;

/// <summary>Keeps the weather as it is while the running script is calm.</summary>
[HarmonyPatch(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick))]
public static class CalmWeatherPatch
{
    public static bool Prefix() => ScriptSession.Current is not { Calm: true };
}
