namespace ArchotechHarness.Cli;

/// <summary>An official expansion, by its short scenario name and package id.</summary>
public sealed record Dlc(string Name, string PackageId)
{
    public static IReadOnlyList<Dlc> All { get; } =
    [
        new("royalty", "ludeon.rimworld.royalty"),
        new("ideology", "ludeon.rimworld.ideology"),
        new("biotech", "ludeon.rimworld.biotech"),
        new("anomaly", "ludeon.rimworld.anomaly"),
        new("odyssey", "ludeon.rimworld.odyssey"),
    ];

    public static Dlc Named(string name) =>
        All.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(d.PackageId, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ScenarioException($"Unknown DLC '{name}'. Use one of: {string.Join(", ", All.Select(d => d.Name))}.");
}
