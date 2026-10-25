using System;

namespace ArchotechHarness.Scripting;

/// <summary>DLCs the scenario needs: royalty, ideology, biotech, anomaly or odyssey.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DlcAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names;
}
