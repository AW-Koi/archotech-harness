namespace DevHarness.Cli;

/// <summary>A line of a script file with its 1-based line number and the file it came from.</summary>
public sealed record ScriptLine(string File, int Number, string Text);
