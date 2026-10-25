using System;
using System.Reflection;

namespace ArchotechHarness.Scripting;

/// <summary>The seed a scenario runs with: its [Seed], or a fresh one to report.</summary>
public static class ScenarioSeed
{
    public static int Resolve(Type scenario, Func<int> fresh) =>
        scenario.GetCustomAttribute<SeedAttribute>()?.Seed ?? fresh();
}
