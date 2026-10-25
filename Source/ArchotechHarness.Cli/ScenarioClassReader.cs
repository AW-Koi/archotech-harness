using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchotechHarness.Cli;

/// <summary>Reads a scenario's class name and its [Mod], [RequiresMod], [Dlc], [Window] and [Biome] attributes from source, before anything builds.</summary>
/// <remarks>Attribute arguments must be literals, or nameof for [Biome]. Paths are relative to the scenario file; the project's mods come first.</remarks>
public static class ScenarioClassReader
{
    public const string BaseClassName = "HarnessScenario";

    public static ScenarioClass Read(string sourceFile) =>
        ReadAll(sourceFile).FirstOrDefault()
        ?? throw new ScenarioException($"{Path.GetFullPath(sourceFile)} has no class deriving from {BaseClassName}.");

    /// <summary>Every scenario class in the file, in source order; none when it holds no scenario.</summary>
    public static IReadOnlyList<ScenarioClass> ReadAll(string sourceFile)
    {
        var fullPath = Path.GetFullPath(sourceFile);
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(fullPath), path: fullPath).GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.BaseList?.Types.Any(t => LastName(t.Type) == BaseClassName) == true)
            .ToList();
        if (classes.Count == 0)
        {
            return [];
        }

        var project = FindProject(Path.GetDirectoryName(fullPath)!, fullPath);
        var projectMods = ProjectModReader.Read(project);
        return classes.Select(c => ReadClass(c, fullPath, project, projectMods)).ToList();
    }

    private static ScenarioClass ReadClass(
        ClassDeclarationSyntax scenario, string fullPath, string project, IReadOnlyList<ScenarioMod> projectMods)
    {
        var directory = Path.GetDirectoryName(fullPath)!;
        var mods = new List<ScenarioMod>(projectMods);
        var dlcs = new List<string>();
        var requiredMods = new List<string>();
        var window = new WindowSize(1600, 900);
        string? biome = null;
        foreach (var attribute in scenario.AttributeLists.SelectMany(l => l.Attributes))
        {
            var arguments = attribute.ArgumentList?.Arguments.ToList() ?? [];
            switch (LastName(attribute.Name).Replace("Attribute", ""))
            {
                case "Mod":
                    mods.Add(ReadMod(arguments, directory, fullPath));
                    break;
                case "RequiresMod":
                    requiredMods.AddRange(arguments.Select(a => Literal<string>(a, fullPath)));
                    break;
                case "Dlc":
                    dlcs.AddRange(arguments.Select(a => Literal<string>(a, fullPath)));
                    break;
                case "Window":
                    window = new WindowSize(Literal<int>(arguments[0], fullPath), Literal<int>(arguments[1], fullPath));
                    break;
                case "Biome":
                    biome = NameOrLiteral(arguments[0], fullPath);
                    break;
            }
        }

        var distinctMods = mods.DistinctBy(m => m.Directory, StringComparer.OrdinalIgnoreCase).ToList();
        var distinctRequiredMods = requiredMods.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new ScenarioClass(FullName(scenario), fullPath, project, distinctMods, dlcs, window, distinctRequiredMods, biome);
    }

    private static ScenarioMod ReadMod(List<AttributeArgumentSyntax> arguments, string directory, string file)
    {
        var modDirectory = ModPath.Resolve(directory, Literal<string>(arguments.First(a => a.NameEquals == null), file));
        var named = arguments.Where(a => a.NameEquals != null)
            .ToDictionary(a => a.NameEquals!.Name.Identifier.Text, a => Literal<string>(a, file));
        var build = named.TryGetValue("Build", out var command)
            ? new BuildCommand(command, named.TryGetValue("BuildIn", out var buildIn) ? ModPath.Resolve(directory, buildIn) : modDirectory)
            : null;
        return new ScenarioMod(modDirectory, build);
    }

    private static T Literal<T>(AttributeArgumentSyntax argument, string file) =>
        argument.Expression is LiteralExpressionSyntax { Token.Value: T value }
            ? value
            : throw new ScenarioException($"{file}: scenario attribute arguments must be literals, not '{argument.Expression}'.");

    private static string NameOrLiteral(AttributeArgumentSyntax argument, string file) =>
        argument.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } nameOf
            ? LastName(nameOf.ArgumentList.Arguments[0].Expression)
            : Literal<string>(argument, file);

    private static string FullName(ClassDeclarationSyntax type)
    {
        var names = new List<string> { type.Identifier.Text };
        for (var parent = type.Parent; parent != null; parent = parent.Parent)
        {
            switch (parent)
            {
                case TypeDeclarationSyntax outer:
                    names.Insert(0, outer.Identifier.Text + "+");
                    break;
                case BaseNamespaceDeclarationSyntax ns:
                    names.Insert(0, ns.Name + ".");
                    break;
            }
        }

        return string.Concat(names);
    }

    private static string FindProject(string directory, string file)
    {
        for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
        {
            var projects = current.GetFiles("*.csproj");
            if (projects.Length > 0)
            {
                return projects[0].FullName;
            }
        }

        throw new ScenarioException($"{file} is not inside a project; scenarios belong to a scenarios .csproj.");
    }

    private static string LastName(SyntaxNode name) => name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => name.ToString(),
    };
}
