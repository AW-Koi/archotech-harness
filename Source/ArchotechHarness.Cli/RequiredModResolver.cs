namespace ArchotechHarness.Cli;

/// <summary>Finds required mods by package id in mod folders, such as the Mods folder and Steam workshop folders, and adds their dependencies.</summary>
/// <remarks>Earlier folders win. Harmony, Core and the harness are left to the profile; DLC dependencies become DLCs.</remarks>
public sealed class RequiredModResolver(IReadOnlyList<string> searchFolders, string gameVersion)
{
    private static readonly HashSet<string> ProfileMods = [ModProfile.HarmonyPackageId, ModProfile.CorePackageId, ModProfile.HarnessPackageId];

    private readonly Dictionary<string, ModMetadata?> _found = new();

    public RequiredModSet Resolve(IReadOnlyList<string> packageIds)
    {
        var mods = new List<ModMetadata>();
        var dlcs = new List<string>();
        var seen = new HashSet<string>();
        var pending = new Queue<(string PackageId, string Declared, ModMetadata? NeededBy)>(packageIds.Select(id => (id.ToLowerInvariant(), id, (ModMetadata?)null)));
        while (pending.TryDequeue(out var next))
        {
            if (!seen.Add(next.PackageId) || ProfileMods.Contains(next.PackageId))
            {
                continue;
            }

            if (Dlc.All.FirstOrDefault(d => d.PackageId == next.PackageId) is { } dlc)
            {
                dlcs.Add(dlc.Name);
                continue;
            }

            if (Find(next.PackageId) is not { } mod)
            {
                return RequiredModSet.Missing(next.NeededBy == null
                    ? $"{next.Declared} is not installed"
                    : $"{next.Declared} is not installed ({next.NeededBy.Name} depends on it)");
            }

            mods.Add(mod);
            foreach (var dependency in mod.Dependencies)
            {
                pending.Enqueue((dependency, dependency, mod));
            }
        }

        return new RequiredModSet(mods, dlcs, null);
    }

    private ModMetadata? Find(string packageId)
    {
        if (!_found.TryGetValue(packageId, out var mod))
        {
            var directory = searchFolders.Select(folder => ModFolder.FindInstalled(folder, packageId)).FirstOrDefault(d => d != null);
            mod = directory == null ? null : ModMetadata.Read(directory, gameVersion);
            _found[packageId] = mod;
        }

        return mod;
    }
}
