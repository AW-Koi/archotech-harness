namespace ArchotechHarness.Scripting;

/// <summary>One distinct error the game logged during a scenario, and how many times.</summary>
public sealed record GameError(string Text, int Count);
