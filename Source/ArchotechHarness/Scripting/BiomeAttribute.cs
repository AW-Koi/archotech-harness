using System;

namespace ArchotechHarness.Scripting;

/// <summary>Starts the colony on a tile of this biome, flat-ish and 10 to 25C on average where the world has one.</summary>
/// <remarks>Takes a BiomeDef name: write nameof(BiomeDefOf.TemperateForest) so the compiler checks it.</remarks>
// ReSharper disable UnusedAutoPropertyAccessor.Global
[AttributeUsage(AttributeTargets.Class)]
public sealed class BiomeAttribute(string defName) : Attribute
{
    public string DefName { get; } = defName;
}
