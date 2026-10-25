namespace DevHarness.Cli;

/// <summary>A line of a script file with its 1-based line number.</summary>
public sealed record ScriptLine(int Number, string Text);
