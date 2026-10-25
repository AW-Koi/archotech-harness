namespace ArchotechHarness.Cli;

/// <summary>Finds the RimWorld install: RIMWORLD_DIR if set, otherwise the Steam library that holds app 294100.</summary>
public static class RimWorldLocator
{
    public const string SteamAppId = "294100";

    public static string Find()
    {
        var configured = Environment.GetEnvironmentVariable("RIMWORLD_DIR");
        if (!string.IsNullOrEmpty(configured))
        {
            return HasGame(configured)
                ? Path.GetFullPath(configured)
                : throw new ScenarioException($"RIMWORLD_DIR is '{configured}', which has no Assembly-CSharp.dll. Set RIMWORLD_DIR to the RimWorld install folder.");
        }

        var fromSteam = FromSteam();
        return fromSteam ?? throw new ScenarioException(
            "RimWorld not found in any Steam library. Set the RIMWORLD_DIR environment variable to the folder that holds Assembly-CSharp.dll.");
    }

    private static string? FromSteam()
    {
        if (SteamLibraries.Installed().FirstOrDefault(l => l.AppIds.Contains(SteamAppId)) is not { } library)
        {
            return null;
        }

        var game = Path.Combine(library.Path, "steamapps", "common", "RimWorld");
        return HasGame(game) ? Path.GetFullPath(game) : null;
    }

    private static bool HasGame(string directory) => File.Exists(Path.Combine(directory, "Assembly-CSharp.dll"));
}
