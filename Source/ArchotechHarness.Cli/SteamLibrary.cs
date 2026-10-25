namespace ArchotechHarness.Cli;

/// <summary>A Steam library folder and the app ids installed in it.</summary>
public sealed record SteamLibrary(string Path, IReadOnlyList<string> AppIds);
