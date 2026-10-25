namespace DevHarness.Cli;

/// <summary>Builds the active mod list for a harness session.</summary>
public static class ModProfile
{
    public const string HarmonyPackageId = "brrainz.harmony";
    public const string CorePackageId = "ludeon.rimworld";
    public const string HarnessPackageId = "terraincognita.rimworlddevharness";

    public static IReadOnlyList<string> LoadOrder(IEnumerable<string> dlcs, IEnumerable<string> modsUnderTest)
    {
        var requested = dlcs.Select(Dlc.Named).ToHashSet();
        var order = new List<string> { HarmonyPackageId, CorePackageId };
        order.AddRange(Dlc.All.Where(requested.Contains).Select(d => d.PackageId));
        order.Add(HarnessPackageId);
        order.AddRange(modsUnderTest.Select(id => id.ToLowerInvariant()).Where(id => !order.Contains(id)));
        return order;
    }
}
