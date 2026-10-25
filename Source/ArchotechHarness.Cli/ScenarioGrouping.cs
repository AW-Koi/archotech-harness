namespace ArchotechHarness.Cli;

/// <summary>Splits scenarios into boots by their mod and DLC set, keeping the order groups first appear in.</summary>
public static class ScenarioGrouping
{
    public static IReadOnlyList<ScenarioGroup> Group(IEnumerable<ScenarioClass> scenarios) =>
        scenarios
            .GroupBy(Key)
            .Select(group =>
            {
                var first = group.First();
                return new ScenarioGroup(
                    Name(first),
                    first.Mods.OrderBy(m => m.Directory, StringComparer.OrdinalIgnoreCase).ToList(),
                    first.Dlcs.Select(d => d.ToLowerInvariant()).Order().ToList(),
                    first.Window,
                    group.ToList());
            })
            .ToList();

    private static string Key(ScenarioClass scenario) =>
        string.Join('|', scenario.Mods.Select(m => m.Directory.ToLowerInvariant()).Order())
        + "#" + string.Join('|', scenario.Dlcs.Select(d => d.ToLowerInvariant()).Order())
        + "#" + scenario.Window;

    private static string Name(ScenarioClass scenario)
    {
        var parts = scenario.Mods.Select(m => ModName(m.Directory)).Order(StringComparer.OrdinalIgnoreCase)
            .Concat(scenario.Dlcs.Select(d => d.ToLowerInvariant()).Order())
            .ToList();
        return parts.Count == 0 ? "core" : string.Join('+', parts);
    }

    private static string ModName(string directory)
    {
        var name = Path.GetFileName(directory);
        return name.Equals("mod", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(Path.GetDirectoryName(directory)!) : name;
    }
}
