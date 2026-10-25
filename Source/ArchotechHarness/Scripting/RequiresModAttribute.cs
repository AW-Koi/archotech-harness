using System;

namespace ArchotechHarness.Scripting;

/// <summary>An installed mod the scenario loads besides the mods under test, by package id, with its dependencies.</summary>
/// <remarks>Found in the Mods folder or a Steam workshop folder. The scenario is skipped when it isn't installed.</remarks>
// ReSharper disable UnusedAutoPropertyAccessor.Global
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequiresModAttribute(string packageId) : Attribute
{
    public string PackageId { get; } = packageId;
}
