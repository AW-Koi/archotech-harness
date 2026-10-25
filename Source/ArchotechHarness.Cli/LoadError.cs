namespace ArchotechHarness.Cli;

/// <summary>An error the game logged while it loaded, with the mod it names, if any.</summary>
public sealed record LoadError(string Text, string? Mod, bool FromModUnderTest);
