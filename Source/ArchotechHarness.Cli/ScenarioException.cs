namespace ArchotechHarness.Cli;

/// <summary>A scenario or profile the harness cannot run as written.</summary>
public class ScenarioException(string message) : Exception(message);
