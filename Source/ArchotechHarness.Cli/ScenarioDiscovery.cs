namespace ArchotechHarness.Cli;

/// <summary>Finds scenario classes in source files, folders or scenarios projects, skipping bin and obj.</summary>
public static class ScenarioDiscovery
{
    private static readonly string[] BuildOutputFolders = ["bin", "obj"];

    public static IReadOnlyList<ScenarioClass> Discover(IEnumerable<string> paths)
    {
        var scenarios = new List<ScenarioClass>();
        foreach (var path in paths.Select(Path.GetFullPath))
        {
            if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                scenarios.AddRange(ScenarioClassReader.ReadAll(path));
            }
            else if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                scenarios.AddRange(FromFolder(Path.GetDirectoryName(path)!));
            }
            else if (Directory.Exists(path))
            {
                scenarios.AddRange(FromFolder(path));
            }
            else
            {
                throw new ScenarioException($"{path} is not a scenario file, scenarios project or folder.");
            }
        }

        return scenarios.DistinctBy(s => s.TypeName).ToList();
    }

    private static IEnumerable<ScenarioClass> FromFolder(string folder) =>
        Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(folder, file).Split(Path.DirectorySeparatorChar)
                .Any(part => BuildOutputFolders.Contains(part, StringComparer.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .SelectMany(ScenarioClassReader.ReadAll);
}
