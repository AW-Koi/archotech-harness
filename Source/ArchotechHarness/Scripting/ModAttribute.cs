using System;

namespace ArchotechHarness.Scripting;

/// <summary>A mod under test: the folder holding About/About.xml, relative to the scenario file.</summary>
/// <remarks>Build runs as a shell command in BuildIn (default: the mod folder) before launch. Arguments must be literals.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ModAttribute(string directory) : Attribute
{
    public string Directory { get; } = directory;

    public string Build { get; set; }

    public string BuildIn { get; set; }
}
