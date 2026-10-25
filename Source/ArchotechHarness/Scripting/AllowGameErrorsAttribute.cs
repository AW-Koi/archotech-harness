using System;

namespace ArchotechHarness.Scripting;

/// <summary>Lets the scenario pass although the game logs errors; with text, only errors containing it (case-sensitive).</summary>
// ReSharper disable UnusedAutoPropertyAccessor.Global
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AllowGameErrorsAttribute(string containing = null) : Attribute
{
    public string Containing { get; } = containing;
}
