namespace ArchotechHarness.Cli;

/// <summary>Another archo session, or a RimWorld outside archo, already holds the game.</summary>
public sealed class SessionBusyException(string message) : ScenarioException(message);
