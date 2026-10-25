using System.Runtime.Loader;

namespace DevHarness.Cli.Tests;

public sealed class ScriptCompilerTests : IDisposable
{
    private static readonly string[] PlainUsings = ["System", "System.Threading.Tasks"];

    private readonly TempDirectory _temp = new();
    private readonly ScriptCompiler _compiler = new(PlatformReferences(), PlainUsings);

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Compile_AsyncTopLevelCode_RunsThroughTheEntryPoint()
    {
        var helper = _temp.WriteFile("helpers.cs", "public static class Recorder { public static int Value; }");
        var path = _temp.WriteFile("ok.csx", $"#load \"helpers.cs\"\nawait Task.Yield();\nRecorder.Value = 42;\n");

        var compiled = _compiler.Compile(ScriptSource.Parse(path));

        var assembly = new AssemblyLoadContext("script", isCollectible: true).LoadFromStream(new MemoryStream(compiled.Assembly));
        await (Task)assembly.GetType(compiled.EntryType)!.GetMethod(ScriptCompiler.EntryMethod)!.Invoke(null, null)!;
        Assert.Equal(42, (int)assembly.GetType("Recorder")!.GetField("Value")!.GetValue(null)!);
        Assert.True(File.Exists(helper));
    }

    [Fact]
    public void Compile_TypeErrorInTheScript_ReportsTheScriptFileAndLine()
    {
        var path = _temp.WriteFile("broken.csx", "// @dlc biotech\nvar room = 1;\nint mop = \"wet\";\n");

        var error = Assert.Throws<ScriptCompileException>(() => _compiler.Compile(ScriptSource.Parse(path)));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.StartsWith($"{path}(3,", diagnostic);
    }

    [Fact]
    public void Compile_UsingLineInTheScript_AppliesToTheBody()
    {
        var path = _temp.WriteFile("usings.csx", "using System.Text;\nvar text = new StringBuilder(\"mop\").ToString();\n");

        var compiled = _compiler.Compile(ScriptSource.Parse(path));

        Assert.NotEmpty(compiled.Assembly);
    }

    private static IReadOnlyList<string> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
}
