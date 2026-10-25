using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DevHarness.Cli;

/// <summary>Compiles a .csx script into an assembly whose entry type has a static async Run method.</summary>
/// <remarks>#line directives map diagnostics and stack traces back to the script file.</remarks>
public sealed class ScriptCompiler(IEnumerable<string> referencePaths, IEnumerable<string> defaultUsings)
{
    public const string EntryMethod = "Run";

    private readonly IReadOnlyList<MetadataReference> _references = referencePaths
        .Where(IsManagedAssembly)
        .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
        .Select(group => (MetadataReference)MetadataReference.CreateFromFile(group.First()))
        .ToList();

    private readonly IReadOnlyList<string> _defaultUsings = defaultUsings.ToList();

    public CompiledScript Compile(ScriptSource script)
    {
        var assemblyName = "HarnessScript_" + Guid.NewGuid().ToString("N");
        var entryType = assemblyName + ".Entry";
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText(Wrap(script, assemblyName), parse, script.Path, Encoding.UTF8),
        };
        trees.AddRange(script.HelperFiles.Select(file =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, file, Encoding.UTF8)));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            trees,
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, allowUnsafe: true)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["CS1998"] = ReportDiagnostic.Suppress, ["CS0105"] = ReportDiagnostic.Suppress, ["CS1701"] = ReportDiagnostic.Suppress, ["CS1702"] = ReportDiagnostic.Suppress }));

        using var assembly = new MemoryStream();
        using var symbols = new MemoryStream();
        var result = compilation.Emit(assembly, symbols, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        if (!result.Success)
        {
            throw new ScriptCompileException(result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList());
        }

        return new CompiledScript(assemblyName, entryType, assembly.ToArray(), symbols.ToArray());
    }

    private string Wrap(ScriptSource script, string assemblyName)
    {
        var source = new StringBuilder();
        foreach (var usingDirective in _defaultUsings)
        {
            source.Append($"using {usingDirective};\n");
        }

        foreach (var line in script.Usings)
        {
            source.Append($"#line {line.Number} \"{line.File}\"\n{line.Text}\n");
        }

        source.Append($"#line default\nnamespace {assemblyName}\n{{\npublic static class Entry\n{{\npublic static async System.Threading.Tasks.Task {EntryMethod}()\n{{\n");
        source.Append($"#line 1 \"{script.Path}\"\n{script.Body}\n");
        source.Append("#line default\n}\n}\n}\n");
        return source.ToString();
    }

    public static bool IsManagedAssembly(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new PEReader(stream);
            return reader.HasMetadata && reader.GetMetadataReader().IsAssembly;
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
