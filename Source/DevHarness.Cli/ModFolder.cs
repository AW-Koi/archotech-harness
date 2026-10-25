using System.Xml.Linq;

namespace DevHarness.Cli;

/// <summary>Reads mod metadata and finds installed mods by package id.</summary>
public static class ModFolder
{
    public static string ReadPackageId(string modDirectory)
    {
        var about = Path.Combine(modDirectory, "About", "About.xml");
        if (!File.Exists(about))
        {
            throw new ScenarioException($"{modDirectory} is not a mod folder: About/About.xml is missing.");
        }

        var packageId = XDocument.Load(about).Root?.Element("packageId")?.Value.Trim();
        return string.IsNullOrEmpty(packageId)
            ? throw new ScenarioException($"{about} has no packageId.")
            : packageId.ToLowerInvariant();
    }

    public static string? FindInstalled(string modsDirectory, string packageId)
    {
        if (!Directory.Exists(modsDirectory))
        {
            return null;
        }

        foreach (var candidate in Directory.EnumerateDirectories(modsDirectory))
        {
            try
            {
                if (ReadPackageId(candidate) == packageId.ToLowerInvariant())
                {
                    return candidate;
                }
            }
            catch (Exception e) when (e is ScenarioException or IOException or System.Xml.XmlException or UnauthorizedAccessException)
            {
                // Not a readable mod folder; keep looking.
            }
        }

        return null;
    }
}
