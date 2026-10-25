namespace ArchotechHarness.Cli;

/// <summary>Splits scenarios into boots by their mods under test, required mods, DLC set and biome, keeping the order groups first appear in.</summary>
public static class ScenarioGrouping
{
    public static IReadOnlyList<ScenarioGroup> Group(IEnumerable<ScenarioClass> scenarios, RequiredModResolver resolver) =>
        scenarios
            .Select(scenario => new Profile(scenario, resolver.Resolve(scenario.RequiredMods)))
            .GroupBy(Key)
            .Select(group =>
            {
                var first = group.First();
                return new ScenarioGroup(
                    Name(first),
                    first.Scenario.Mods.OrderBy(m => m.Directory, StringComparer.OrdinalIgnoreCase).ToList(),
                    first.Required.Mods,
                    first.Dlcs,
                    first.Scenario.Window,
                    group.Select(p => p.Scenario).ToList(),
                    first.Required.SkipReason,
                    first.Scenario.Biome);
            })
            .ToList();

    private static string Key(Profile profile) =>
        string.Join('|', profile.Scenario.Mods.Select(m => m.Directory.ToLowerInvariant()).Order())
        + "#" + string.Join('|', profile.Required.Mods.Select(m => m.PackageId).Order())
        + "#" + string.Join('|', profile.Dlcs)
        + "#" + profile.Scenario.Window
        + "#" + profile.Required.SkipReason
        + "#" + profile.Scenario.Biome;

    private static string Name(Profile profile)
    {
        var parts = profile.Scenario.Mods.Select(m => ModName(m.Directory)).Order(StringComparer.OrdinalIgnoreCase)
            .Concat(profile.Scenario.RequiredMods.Select(id => id.ToLowerInvariant()).Distinct().Order())
            .Concat(profile.Dlcs)
            .Concat(profile.Scenario.Biome is { } biome ? [biome] : [])
            .ToList();
        return parts.Count == 0 ? "core" : string.Join('+', parts);
    }

    private static string ModName(string directory)
    {
        var name = Path.GetFileName(directory);
        return name.Equals("mod", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(Path.GetDirectoryName(directory)!) : name;
    }

    private sealed record Profile(ScenarioClass Scenario, RequiredModSet Required)
    {
        public IReadOnlyList<string> Dlcs { get; } =
            Scenario.Dlcs.Concat(Required.Dlcs).Select(d => d.ToLowerInvariant()).Distinct().Order().ToList();
    }
}
