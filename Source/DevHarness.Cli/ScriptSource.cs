using System.Text.RegularExpressions;

namespace DevHarness.Cli;

/// <summary>A .csx scenario script: its harness directives, hoisted using lines and C# body.</summary>
/// <remarks>Directive and using lines become blank in Body, so compiler line numbers match the file.</remarks>
public sealed partial record ScriptSource(
    string Path,
    string Body,
    IReadOnlyList<ScriptLine> Usings,
    IReadOnlyList<ScenarioMod> Mods,
    IReadOnlyList<string> Dlcs,
    WindowSize Window,
    IReadOnlyList<string> HelperFiles)
{
    private static readonly string[] CSharpDirectives = ["#region", "#endregion", "#if", "#else", "#elif", "#endif", "#pragma", "#nullable"];

    public static ScriptSource Parse(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var baseDirectory = System.IO.Path.GetDirectoryName(fullPath)!;
        var lines = File.ReadAllText(fullPath).Replace("\r\n", "\n").Split('\n');
        var mods = new List<ScenarioMod>();
        var dlcs = new List<string>();
        var helpers = new List<string>();
        var usings = new List<ScriptLine>();
        var window = new WindowSize(1600, 900);
        var inPreamble = true;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var number = i + 1;
            if (line.StartsWith('#') && !CSharpDirectives.Any(d => line.StartsWith(d, StringComparison.Ordinal)))
            {
                ApplyDirective(line, fullPath, number, baseDirectory, mods, dlcs, helpers, ref window);
                lines[i] = "";
            }
            else if (inPreamble && UsingLine().IsMatch(line))
            {
                usings.Add(new ScriptLine(number, line));
                lines[i] = "";
            }
            else if (line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            {
                inPreamble = false;
            }
        }

        return new ScriptSource(fullPath, string.Join('\n', lines), usings, mods, dlcs, window, helpers);
    }

    private static void ApplyDirective(
        string line, string path, int number, string baseDirectory,
        List<ScenarioMod> mods, List<string> dlcs, List<string> helpers, ref WindowSize window)
    {
        Match match;
        if ((match = ModDirective().Match(line)).Success)
        {
            mods.Add(new ScenarioMod(Resolve(baseDirectory, match.Groups[1].Value), null));
        }
        else if ((match = BuildDirective().Match(line)).Success)
        {
            if (mods.Count == 0)
            {
                throw new ScenarioException($"{path}({number}): #build must follow the #mod it builds.");
            }

            mods[^1] = mods[^1] with { Build = new BuildCommand(match.Groups[1].Value, Resolve(baseDirectory, match.Groups[2].Value)) };
        }
        else if ((match = DlcDirective().Match(line)).Success)
        {
            dlcs.AddRange(match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else if ((match = WindowDirective().Match(line)).Success)
        {
            window = new WindowSize(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }
        else if ((match = LoadDirective().Match(line)).Success)
        {
            helpers.Add(Resolve(baseDirectory, match.Groups[1].Value));
        }
        else
        {
            throw new ScenarioException($"{path}({number}): unknown directive '{line}'. Use #mod, #build, #dlc, #window or #load.");
        }
    }

    private static string Resolve(string baseDirectory, string path) =>
        System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(baseDirectory, path))
            .TrimEnd(System.IO.Path.DirectorySeparatorChar);

    [GeneratedRegex("""^#mod\s+"([^"]+)"\s*$""")]
    private static partial Regex ModDirective();

    [GeneratedRegex("""^#build\s+"([^"]+)"\s+in\s+"([^"]+)"\s*$""")]
    private static partial Regex BuildDirective();

    [GeneratedRegex("""^#dlc\s+([\w,\s]+)$""")]
    private static partial Regex DlcDirective();

    [GeneratedRegex("""^#window\s+(\d+)x(\d+)\s*$""")]
    private static partial Regex WindowDirective();

    [GeneratedRegex("""^#load\s+"([^"]+)"\s*$""")]
    private static partial Regex LoadDirective();

    [GeneratedRegex("""^using\s+(static\s+)?[\w.]+(\s*=\s*[\w.<>, ]+)?\s*;$""")]
    private static partial Regex UsingLine();
}
