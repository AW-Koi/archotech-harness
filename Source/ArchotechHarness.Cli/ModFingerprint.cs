using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;

namespace ArchotechHarness.Cli;

/// <summary>What names a mod in the game's log: its name, package id, folders, defs, def files and code namespaces.</summary>
public sealed record ModFingerprint(
    string Name,
    string PackageId,
    IReadOnlyList<string> Directories,
    IReadOnlySet<string> DefNames,
    IReadOnlySet<string> DefFiles,
    IReadOnlySet<string> Namespaces)
{
    private static readonly HashSet<string> GameNamespaceRoots =
        ["RimWorld", "Verse", "System", "Unity", "UnityEngine", "HarmonyLib", "Microsoft", "Mono", "LudeonTK", "Brutal"];

    private static readonly HashSet<string> SkippedFolders = new(["About", "Languages", "bin", "obj", ".git"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Reads a mod folder; installedDirectory is where the game sees it, when that differs.</summary>
    public static ModFingerprint Read(string modDirectory, string? installedDirectory)
    {
        var packageId = ModFolder.ReadPackageId(modDirectory);
        var name = XDocument.Load(Path.Combine(modDirectory, "About", "About.xml")).Root?.Element("name")?.Value.Trim();
        var defNames = new HashSet<string>(StringComparer.Ordinal);
        var defFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Files(modDirectory))
        {
            if (file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                ReadXml(file, defNames, defFiles);
            }
            else if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                     && Path.GetFileName(Path.GetDirectoryName(file))?.Equals("Assemblies", StringComparison.OrdinalIgnoreCase) == true)
            {
                ReadNamespaces(file, namespaces);
            }
        }

        var directories = new[] { modDirectory, installedDirectory }.OfType<string>()
            .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new ModFingerprint(string.IsNullOrEmpty(name) ? packageId : name, packageId, directories, defNames, defFiles, namespaces);
    }

    private static IEnumerable<string> Files(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(directory).Where(d => !SkippedFolders.Contains(Path.GetFileName(d))))
        {
            foreach (var file in Files(child))
            {
                yield return file;
            }
        }
    }

    private static void ReadXml(string file, HashSet<string> defNames, HashSet<string> defFiles)
    {
        try
        {
            var root = XDocument.Load(file).Root;
            if (root?.Name.LocalName == "Defs")
            {
                defFiles.Add(Path.GetFileName(file));
            }

            defNames.UnionWith(root?.Descendants("defName").Select(d => d.Value.Trim()).Where(d => d.Length > 0) ?? []);
        }
        catch (XmlException)
        {
            // The game logs a broken file itself.
        }
    }

    private static void ReadNamespaces(string assembly, HashSet<string> namespaces)
    {
        try
        {
            using var stream = File.OpenRead(assembly);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                return;
            }

            var metadata = pe.GetMetadataReader();
            foreach (var handle in metadata.TypeDefinitions)
            {
                var ns = metadata.GetString(metadata.GetTypeDefinition(handle).Namespace);
                if (ns.Length > 0 && !GameNamespaceRoots.Contains(ns.Split('.')[0]))
                {
                    namespaces.Add(ns);
                }
            }
        }
        catch (BadImageFormatException)
        {
            // Not a .NET assembly.
        }
    }
}
