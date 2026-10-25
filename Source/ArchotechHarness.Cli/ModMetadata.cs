using System.Xml.Linq;

namespace ArchotechHarness.Cli;

/// <summary>A mod's About.xml: its package id, name, dependencies and load order hints, with package ids lower-cased.</summary>
/// <remarks>The *ByVersion lists add the entries for the running game version.</remarks>
public sealed record ModMetadata(
    string PackageId,
    string Name,
    string Directory,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> LoadAfter,
    IReadOnlyList<string> LoadBefore)
{
    public static ModMetadata Read(string modDirectory, string gameVersion)
    {
        var packageId = ModFolder.ReadPackageId(modDirectory);
        var root = XDocument.Load(Path.Combine(modDirectory, "About", "About.xml")).Root!;
        var versionKey = "v" + string.Join('.', gameVersion.Split(' ')[0].Split('.').Take(2));
        var name = root.Element("name")?.Value.Trim();
        return new ModMetadata(
            packageId,
            string.IsNullOrEmpty(name) ? packageId : name,
            modDirectory,
            PackageIds(root.Element("modDependencies"))
                .Concat(PackageIds(root.Element("modDependenciesByVersion")?.Element(versionKey))).Distinct().ToList(),
            List(root, "loadAfter", versionKey),
            List(root, "loadBefore", versionKey));
    }

    private static IEnumerable<string> PackageIds(XElement? list) =>
        list?.Elements("li").Select(li => li.Element("packageId")?.Value.Trim().ToLowerInvariant()).OfType<string>().Where(id => id.Length > 0) ?? [];

    private static List<string> List(XElement root, string name, string versionKey) =>
        Items(root.Element(name)).Concat(Items(root.Element(name + "ByVersion")?.Element(versionKey))).Distinct().ToList();

    private static IEnumerable<string> Items(XElement? list) =>
        list?.Elements("li").Select(li => li.Value.Trim().ToLowerInvariant()).Where(id => id.Length > 0) ?? [];
}
