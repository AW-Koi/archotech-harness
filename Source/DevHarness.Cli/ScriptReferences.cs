namespace DevHarness.Cli;

/// <summary>Finds the assemblies a script compiles against: the game, Harmony, the harness and the mods under test.</summary>
public static class ScriptReferences
{
    public const string HarmonyPackageId = "brrainz.harmony";

    public static readonly string[] DefaultUsings =
    [
        "System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks",
        "Verse", "Verse.AI", "RimWorld", "DevHarness.Scripting", "static DevHarness.Scripting.Script",
    ];

    public static IReadOnlyList<string> Collect(HarnessPaths paths, IEnumerable<ScenarioMod> mods)
    {
        var references = Directory.EnumerateFiles(paths.GameDirectory, "*.dll").ToList();
        var modDirectories = new List<string> { paths.HarnessModDirectory };
        modDirectories.AddRange(mods.Select(m => m.Directory));
        var harmony = ModFolder.FindInstalled(paths.ModsDirectory, HarmonyPackageId)
            ?? ModFolder.FindInstalled(paths.WorkshopDirectory, HarmonyPackageId);
        if (harmony != null)
        {
            modDirectories.Add(harmony);
        }

        foreach (var directory in modDirectories)
        {
            foreach (var assemblies in new[] { Path.Combine(directory, "Assemblies"), Path.Combine(directory, "1.7", "Assemblies") })
            {
                if (Directory.Exists(assemblies))
                {
                    references.AddRange(Directory.EnumerateFiles(assemblies, "*.dll"));
                }
            }
        }

        return references;
    }
}
