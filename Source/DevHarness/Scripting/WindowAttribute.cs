using System;

namespace DevHarness.Scripting;

/// <summary>The game window size for the scenario; the default is 1600 by 900.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WindowAttribute(int width, int height) : Attribute
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
