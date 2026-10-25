using System.Text;
using Microsoft.Win32;

namespace ArchotechHarness.Cli;

/// <summary>Reads Steam's libraryfolders.vdf: which library folders exist and which apps each holds.</summary>
public static class SteamLibraries
{
    /// <summary>The libraries of the Steam install in the registry; none when Steam is not installed.</summary>
    public static IReadOnlyList<SteamLibrary> Installed()
    {
        if (!OperatingSystem.IsWindows() || Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is not string steam)
        {
            return [];
        }

        var libraries = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        return File.Exists(libraries) ? Parse(File.ReadAllText(libraries)) : [];
    }

    public static IReadOnlyList<SteamLibrary> Parse(string vdf)
    {
        var tokens = Tokenize(vdf);
        var position = 0;
        var root = ReadObject(tokens, ref position);
        if (root.GetValueOrDefault("libraryfolders") is not Dictionary<string, object> folders)
        {
            return [];
        }

        return folders.Values.OfType<Dictionary<string, object>>()
            .Where(folder => folder.GetValueOrDefault("path") is string)
            .Select(folder => new SteamLibrary(
                (string)folder["path"],
                folder.GetValueOrDefault("apps") is Dictionary<string, object> apps ? apps.Keys.ToList() : []))
            .ToList();
    }

    public static string? LibraryWithApp(string vdf, string appId) =>
        Parse(vdf).FirstOrDefault(library => library.AppIds.Contains(appId))?.Path;

    private static Dictionary<string, object> ReadObject(List<Token> tokens, ref int position)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (position + 1 < tokens.Count && tokens[position].Kind == TokenKind.Text)
        {
            var key = tokens[position++].Text;
            var next = tokens[position++];
            result[key] = next.Kind == TokenKind.Open ? ReadObject(tokens, ref position) : next.Text;
        }

        if (position < tokens.Count && tokens[position].Kind == TokenKind.Close)
        {
            position++;
        }

        return result;
    }

    private static List<Token> Tokenize(string vdf)
    {
        var tokens = new List<Token>();
        for (var i = 0; i < vdf.Length; i++)
        {
            switch (vdf[i])
            {
                case '{':
                    tokens.Add(new Token(TokenKind.Open, ""));
                    break;
                case '}':
                    tokens.Add(new Token(TokenKind.Close, ""));
                    break;
                case '"':
                    var text = new StringBuilder();
                    for (i++; i < vdf.Length && vdf[i] != '"'; i++)
                    {
                        if (vdf[i] == '\\' && i + 1 < vdf.Length)
                        {
                            i++;
                        }

                        text.Append(vdf[i]);
                    }

                    tokens.Add(new Token(TokenKind.Text, text.ToString()));
                    break;
            }
        }

        return tokens;
    }

    private enum TokenKind
    {
        Text,
        Open,
        Close,
    }

    private sealed record Token(TokenKind Kind, string Text);
}
