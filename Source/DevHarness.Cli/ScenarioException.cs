namespace DevHarness.Cli;

/// <summary>A scenario or profile the harness cannot run as written.</summary>
public sealed class ScenarioException(string message) : Exception(message);
