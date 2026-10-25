using System.Text.RegularExpressions;

namespace ArchotechHarness.Cli;

/// <summary>Works out which mod a load error comes from, by the mod names, defs, files, folders and namespaces it mentions.</summary>
public static partial class LoadErrorAttribution
{
    public static LoadError Attribute(string text, IReadOnlyList<ModFingerprint> modsUnderTest)
    {
        var prefix = Prefix().Match(text);
        var prefixMod = prefix.Success ? prefix.Groups["mod"].Value : null;
        var culprit = modsUnderTest.FirstOrDefault(mod => Names(text, prefixMod, mod));
        return culprit != null
            ? new LoadError(text, culprit.Name, FromModUnderTest: true)
            : new LoadError(text, prefixMod, FromModUnderTest: false);
    }

    private static bool Names(string text, string? prefixMod, ModFingerprint mod)
    {
        if (prefixMod != null && (prefixMod.Equals(mod.Name, StringComparison.OrdinalIgnoreCase) || prefixMod.Equals(mod.PackageId, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (DefName().Matches(text).Any(m => mod.DefNames.Contains(m.Groups["def"].Value))
            || DefFile().Matches(text).Any(m => mod.DefFiles.Contains(m.Groups["file"].Value)))
        {
            return true;
        }

        var slashed = text.Replace('\\', '/');
        if (mod.Directories.Any(d => slashed.Contains(d.Replace('\\', '/').TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return DottedName().Matches(text).Any(m => NamespacesOf(m.Value).Any(mod.Namespaces.Contains));
    }

    private static IEnumerable<string> NamespacesOf(string dottedName)
    {
        for (var dot = dottedName.LastIndexOf('.'); dot > 0; dot = dottedName.LastIndexOf('.', dot - 1))
        {
            yield return dottedName[..dot];
        }
    }

    [GeneratedRegex(@"^\[(?<mod>[^\]\r\n]+)\]")]
    private static partial Regex Prefix();

    [GeneratedRegex(@"(?:Config error in|ConfigErrors\(\) of|to give to \S+) (?<def>[^\s:]+)")]
    private static partial Regex DefName();

    [GeneratedRegex(@"def from file (?<file>[^\s:]+\.xml)", RegexOptions.IgnoreCase)]
    private static partial Regex DefFile();

    [GeneratedRegex(@"(?<![\w.])[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)+")]
    private static partial Regex DottedName();
}
