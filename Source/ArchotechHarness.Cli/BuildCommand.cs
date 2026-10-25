namespace ArchotechHarness.Cli;

/// <summary>A shell command that builds a mod before launch.</summary>
public sealed record BuildCommand(string Command, string WorkingDirectory);
