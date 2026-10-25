using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace ArchotechHarness.Scripting;

/// <summary>The biome a scenario's [Biome] attribute asks for.</summary>
public static class ScenarioBiome
{
    /// <summary>Null without the attribute; throws when no loaded BiomeDef has that name or colonies can't settle it.</summary>
    public static BiomeDef Resolve(Type scenario)
    {
        if (scenario.GetCustomAttribute<BiomeAttribute>() is not { } attribute)
        {
            return null;
        }

        var biome = DefDatabase<BiomeDef>.GetNamedSilentFail(attribute.DefName)
            ?? throw new HarnessException($"[Biome(\"{attribute.DefName}\")] on {scenario.Name} names no loaded BiomeDef.");
        return biome.canBuildBase
            ? biome
            : throw new HarnessException($"[Biome(\"{attribute.DefName}\")] on {scenario.Name} names a biome colonies can't settle.");
    }
}
