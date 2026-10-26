using HarmonyLib;
using RimWorld;

namespace ArchotechHarness.Scripting;

/// <summary>Keeps the weather as it is while the running script holds the weather.</summary>
[HarmonyPatch(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick))]
public static class CalmWeatherPatch
{
    public static bool Prefix() => ScriptSession.Current is not { HoldWeather: true };
}
