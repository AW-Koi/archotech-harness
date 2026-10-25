using System;

namespace ArchotechHarness.Scripting;

/// <summary>Seeds the game's random numbers before Run(), so a run can be repeated; without it, each run picks a seed and reports it.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SeedAttribute(int seed) : Attribute
{
    public int Seed { get; } = seed;
}
