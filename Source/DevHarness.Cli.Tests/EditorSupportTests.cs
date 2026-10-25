namespace DevHarness.Cli.Tests;

public sealed class EditorSupportTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Write_GameAndModAssemblies_BecomeReferencesWithTheDefaultUsings()
    {
        var gameAssembly = _temp.WriteFile("RimWorld/Assembly-CSharp.dll", "");
        var frameworkAssembly = _temp.WriteFile("RimWorld/System.Runtime.dll", "");
        var path = _temp.Combine("scenarios", EditorSupport.FileName);

        EditorSupport.Write(path, [gameAssembly, frameworkAssembly], ["Verse", "static DevHarness.Scripting.Script"]);

        var lines = File.ReadAllLines(path).Where(l => !l.StartsWith("//")).ToList();
        Assert.Equal([$"#r \"{gameAssembly}\"", "using Verse;", "using static DevHarness.Scripting.Script;"], lines);
    }

    [Fact]
    public void ScriptWithHarnessDirectives_ParsedAsCSharpScript_HasNoSyntaxErrors()
    {
        const string script = """
            // @mod "../MopMod/mod"
            // @build "dotnet build" in "../MopMod"
            #load ".rwh-editor.csx"
            #r "MopMod.dll"
            using System.Text;

            var room = Build.Room(World.Center, width: 11, height: 9);
            await Until(() => room.Filth().Count == 0);
            """;

        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            script, new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(kind: Microsoft.CodeAnalysis.SourceCodeKind.Script));

        Assert.Empty(tree.GetDiagnostics());
    }

    [Fact]
    public void Write_ThenParse_RoundTripsTheUsings()
    {
        var path = _temp.Combine("scenarios", EditorSupport.FileName);
        EditorSupport.Write(path, [], ["Verse", "RimWorld"]);
        var script = _temp.WriteFile("scenarios/mop.csx", $"#load \"{EditorSupport.FileName}\"\nvar room = 1;\n");

        var parsed = ScriptSource.Parse(script);

        Assert.Equal(["using Verse;", "using RimWorld;"], parsed.Usings.Select(u => u.Text));
    }
}
