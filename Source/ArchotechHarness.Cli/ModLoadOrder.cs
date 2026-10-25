namespace ArchotechHarness.Cli;

/// <summary>Orders required mods and mods under test so dependencies and loadAfter targets come first and loadBefore targets later.</summary>
/// <remarks>Otherwise required mods keep their order and load before the mods under test. A cycle falls back to that order.</remarks>
public static class ModLoadOrder
{
    public static IReadOnlyList<string> Sort(IReadOnlyList<ModMetadata> requiredMods, IReadOnlyList<ModMetadata> modsUnderTest)
    {
        var mods = requiredMods.Concat(modsUnderTest).DistinctBy(m => m.PackageId).ToList();
        var ids = mods.Select(m => m.PackageId).ToHashSet();
        var before = mods.ToDictionary(m => m.PackageId, _ => new HashSet<string>());
        foreach (var mod in mods)
        {
            before[mod.PackageId].UnionWith(mod.Dependencies.Concat(mod.LoadAfter).Where(id => ids.Contains(id) && id != mod.PackageId));
            foreach (var later in mod.LoadBefore.Where(id => ids.Contains(id) && id != mod.PackageId))
            {
                before[later].Add(mod.PackageId);
            }
        }

        var order = new List<string>();
        var remaining = mods.Select(m => m.PackageId).ToList();
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(id => before[id].All(order.Contains)) ?? remaining[0];
            order.Add(next);
            remaining.Remove(next);
        }

        return order;
    }
}
