using System.Xml;
using System.Xml.Linq;

namespace ArchotechHarness.Cli;

/// <summary>Reads the &lt;ArchoMod&gt; items every scenario in a scenarios .csproj loads.</summary>
/// <remarks>Plain XML: conditions, imports and properties are not evaluated. Paths are relative to the .csproj.</remarks>
public static class ProjectModReader
{
    public const string ItemName = "ArchoMod";

    public static IReadOnlyList<ScenarioMod> Read(string projectFile)
    {
        var directory = Path.GetDirectoryName(projectFile)!;
        return Load(projectFile).Descendants()
            .Where(e => e.Name.LocalName == ItemName)
            .Select(item => ReadItem(item, directory, projectFile))
            .ToList();
    }

    private static XDocument Load(string projectFile)
    {
        try
        {
            return XDocument.Load(projectFile);
        }
        catch (XmlException e)
        {
            throw new ScenarioException($"{projectFile} is not valid XML: {e.Message}");
        }
    }

    private static ScenarioMod ReadItem(XElement item, string directory, string projectFile)
    {
        var include = (string?)item.Attribute("Include");
        if (string.IsNullOrWhiteSpace(include))
        {
            throw new ScenarioException($"{projectFile}: every <{ItemName}> needs an Include naming the mod folder.");
        }

        var modDirectory = ModPath.Resolve(directory, include);
        var build = (string?)item.Attribute("Build");
        var buildIn = (string?)item.Attribute("BuildIn");
        return new ScenarioMod(
            modDirectory,
            build == null ? null : new BuildCommand(build, buildIn == null ? modDirectory : ModPath.Resolve(directory, buildIn)));
    }
}
