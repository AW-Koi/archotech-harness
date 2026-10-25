namespace ArchotechHarness.Scripting;

/// <summary>One step on a scenario's timeline, with its label formatted once for display.</summary>
public sealed record TimelineStep(double At, int? Tick, string Kind, string Text, int Line, bool? Passed)
{
    public string Label { get; } = Line == 0 ? $"{Kind}: {Text}" : $"{Kind}: {Text} (line {Line})";
}
