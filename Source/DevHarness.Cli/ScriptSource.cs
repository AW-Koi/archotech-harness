using System.Text.RegularExpressions;

namespace DevHarness.Cli;

/// <summary>A .csx scenario script: its harness directives, hoisted using lines and C# body.</summary>
/// <remarks>
/// Harness directives live in comments ("// @mod") so editors accept the file. Directive, #r and using
/// lines become blank in Body, so compiler line numbers match the file.
/// </remarks>
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
            Match match;
            if ((match = HarnessDirective().Match(line)).Success)
            {
                ApplyDirective(match.Groups[1].Value, match.Groups[2].Value.Trim(), $"{fullPath}({number})", baseDirectory, mods, dlcs, ref window);
                lines[i] = "";
            }
            else if (ReferenceDirective().IsMatch(line))
            {
                lines[i] = "";
            }
            else if ((match = LoadDirective().Match(line)).Success)
            {
                Load(Resolve(baseDirectory, match.Groups[1].Value), $"{fullPath}({number})", helpers, usings);
                lines[i] = "";
            }
            else if (line.StartsWith('#') && !CSharpDirectives.Any(d => line.StartsWith(d, StringComparison.Ordinal)))
            {
                throw new ScenarioException($"{fullPath}({number}): unknown directive '{line}'. Harness directives are comments: // @mod, // @build, // @dlc, // @window.");
            }
            else if (inPreamble && UsingLine().IsMatch(line))
            {
                usings.Add(new ScriptLine(fullPath, number, line));
                lines[i] = "";
            }
            else if (line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            {
                inPreamble = false;
            }
        }

        return new ScriptSource(fullPath, string.Join('\n', lines), usings, mods, dlcs, window, helpers);
    }

    private static void Load(string file, string location, List<string> helpers, List<ScriptLine> usings)
    {
        if (!file.EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
        {
            helpers.Add(file);
            return;
        }

        if (!File.Exists(file))
        {
            if (System.IO.Path.GetFileName(file) == EditorSupport.FileName)
            {
                return;
            }

            throw new ScenarioException($"{location}: cannot find {file}.");
        }

        var loaded = Parse(file);
        if (loaded.Body.Split('\n').Any(l => l.Trim().Length > 0 && !l.Trim().StartsWith("//", StringComparison.Ordinal)))
        {
            throw new ScenarioException($"{location}: a loaded .csx may only hold #r and using lines; put shared code in a .cs file.");
        }

        usings.AddRange(loaded.Usings);
        helpers.AddRange(loaded.HelperFiles);
    }

    private static void ApplyDirective(
        string name, string value, string location, string baseDirectory, List<ScenarioMod> mods, List<string> dlcs, ref WindowSize window)
    {
        Match match;
        switch (name)
        {
            case "mod" when (match = QuotedValue().Match(value)).Success:
                mods.Add(new ScenarioMod(Resolve(baseDirectory, match.Groups[1].Value), null));
                break;
            case "build" when (match = BuildValue().Match(value)).Success:
                if (mods.Count == 0)
                {
                    throw new ScenarioException($"{location}: // @build must follow the // @mod it builds.");
                }

                mods[^1] = mods[^1] with { Build = new BuildCommand(match.Groups[1].Value, Resolve(baseDirectory, match.Groups[2].Value)) };
                break;
            case "dlc":
                dlcs.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                break;
            case "window" when (match = WindowValue().Match(value)).Success:
                window = new WindowSize(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                break;
            default:
                throw new ScenarioException($"{location}: cannot read '// @{name} {value}'. Use // @mod \"DIR\", // @build \"COMMAND\" in \"DIR\", // @dlc NAMES or // @window WxH.");
        }
    }

    private static string Resolve(string baseDirectory, string path) =>
        System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(baseDirectory, path))
            .TrimEnd(System.IO.Path.DirectorySeparatorChar);

    [GeneratedRegex("""^//\s*@(\w+)\s*(.*)$""")]
    private static partial Regex HarnessDirective();

    [GeneratedRegex("""^"([^"]+)"$""")]
    private static partial Regex QuotedValue();

    [GeneratedRegex("""^"([^"]+)"\s+in\s+"([^"]+)"$""")]
    private static partial Regex BuildValue();

    [GeneratedRegex("""^(\d+)x(\d+)$""")]
    private static partial Regex WindowValue();

    [GeneratedRegex("""^#r\s+"[^"]+"\s*$""")]
    private static partial Regex ReferenceDirective();

    [GeneratedRegex("""^#load\s+"([^"]+)"\s*$""")]
    private static partial Regex LoadDirective();

    [GeneratedRegex("""^using\s+(static\s+)?[\w.]+(\s*=\s*[\w.<>, ]+)?\s*;$""")]
    private static partial Regex UsingLine();
}
