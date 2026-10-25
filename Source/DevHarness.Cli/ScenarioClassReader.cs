using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevHarness.Cli;

/// <summary>Reads a scenario's class name and its [Mod], [Dlc] and [Window] attributes from source, before anything builds.</summary>
/// <remarks>Attribute arguments must be literals. Paths are relative to the scenario file.</remarks>
public static class ScenarioClassReader
{
    public const string BaseClassName = "HarnessScenario";

    public static ScenarioClass Read(string sourceFile)
    {
        var fullPath = Path.GetFullPath(sourceFile);
        var directory = Path.GetDirectoryName(fullPath)!;
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(fullPath), path: fullPath).GetRoot();
        var scenario = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.BaseList?.Types.Any(t => LastName(t.Type) == BaseClassName) == true)
            ?? throw new ScenarioException($"{fullPath} has no class deriving from {BaseClassName}.");

        var mods = new List<ScenarioMod>();
        var dlcs = new List<string>();
        var window = new WindowSize(1600, 900);
        foreach (var attribute in scenario.AttributeLists.SelectMany(l => l.Attributes))
        {
            var arguments = attribute.ArgumentList?.Arguments.ToList() ?? [];
            switch (LastName(attribute.Name).Replace("Attribute", ""))
            {
                case "Mod":
                    mods.Add(ReadMod(arguments, directory, fullPath));
                    break;
                case "Dlc":
                    dlcs.AddRange(arguments.Select(a => Literal<string>(a, fullPath)));
                    break;
                case "Window":
                    window = new WindowSize(Literal<int>(arguments[0], fullPath), Literal<int>(arguments[1], fullPath));
                    break;
            }
        }

        return new ScenarioClass(FullName(scenario), FindProject(directory, fullPath), mods, dlcs, window);
    }

    private static ScenarioMod ReadMod(List<AttributeArgumentSyntax> arguments, string directory, string file)
    {
        var modDirectory = Resolve(directory, Literal<string>(arguments.First(a => a.NameEquals == null), file));
        var named = arguments.Where(a => a.NameEquals != null)
            .ToDictionary(a => a.NameEquals!.Name.Identifier.Text, a => Literal<string>(a, file));
        var build = named.TryGetValue("Build", out var command)
            ? new BuildCommand(command, named.TryGetValue("BuildIn", out var buildIn) ? Resolve(directory, buildIn) : modDirectory)
            : null;
        return new ScenarioMod(modDirectory, build);
    }

    private static T Literal<T>(AttributeArgumentSyntax argument, string file) =>
        argument.Expression is LiteralExpressionSyntax { Token.Value: T value }
            ? value
            : throw new ScenarioException($"{file}: scenario attribute arguments must be literals, not '{argument.Expression}'.");

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
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => name.ToString(),
    };

    private static string Resolve(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path)).TrimEnd(Path.DirectorySeparatorChar);
}
