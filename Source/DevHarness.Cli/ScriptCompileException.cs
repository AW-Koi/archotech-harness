namespace DevHarness.Cli;

/// <summary>A script that does not compile, with diagnostics as "file(line,column): error ...".</summary>
public sealed class ScriptCompileException(IReadOnlyList<string> diagnostics)
    : Exception("The script does not compile:\n" + string.Join('\n', diagnostics))
{
    public IReadOnlyList<string> Diagnostics { get; } = diagnostics;
}
