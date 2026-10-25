using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>Builds the mods, compiles a .csx scenario, runs it in the game and prints the report.</summary>
/// <remarks>Compiling happens before launch, so a script that does not compile never starts the game.</remarks>
public sealed class ScriptScenarioRunner(HarnessPaths paths, SessionManager sessions, TextWriter output)
{
    private const double ScriptTimeoutSeconds = 1800;

    public bool Run(string scriptPath, string runDirectory, bool keepOpen, int port)
    {
        var script = ScriptSource.Parse(scriptPath);
        var session = sessions.Current;
        if (session == null)
        {
            foreach (var mod in script.Mods.Where(m => m.Build != null))
            {
                sessions.Build(mod.Build!);
            }
        }

        CompiledScript compiled;
        try
        {
            compiled = new ScriptCompiler(ScriptReferences.Collect(paths, script.Mods), ScriptReferences.DefaultUsings).Compile(script);
        }
        catch (ScriptCompileException e)
        {
            output.WriteLine(new JsonObject
            {
                ["passed"] = false,
                ["compileErrors"] = new JsonArray(e.Diagnostics.Select(d => (JsonNode)d).ToArray()),
            }.ToJsonString());
            return false;
        }

        Directory.CreateDirectory(runDirectory);
        var assemblyPath = Path.Combine(runDirectory, compiled.AssemblyName + ".dll");
        File.WriteAllBytes(assemblyPath, compiled.Assembly);
        File.WriteAllBytes(Path.ChangeExtension(assemblyPath, ".pdb"), compiled.Symbols);

        session ??= sessions.Start(new LaunchOptions(
            script.Dlcs, script.Mods.Select(m => m with { Build = null }).ToList(), script.Window, port));
        var response = new HarnessClient(session.Port).Send("runScript", new JsonObject
        {
            ["assembly"] = assemblyPath,
            ["entryType"] = compiled.EntryType,
            ["outputDirectory"] = runDirectory,
            ["timeoutSeconds"] = ScriptTimeoutSeconds,
        });

        var report = response["ok"]?.GetValue<bool>() == true
            ? response["result"]!.AsObject()
            : new JsonObject { ["passed"] = false, ["error"] = response["error"]?.DeepClone() };
        report["script"] = script.Path;
        report["runDirectory"] = runDirectory;
        if (!keepOpen)
        {
            var restore = sessions.Stop();
            report["playerFilesVerified"] = restore?.Verified;
        }

        File.WriteAllText(Path.Combine(runDirectory, "report.json"), report.ToJsonString(new() { WriteIndented = true }));
        output.WriteLine(report.ToJsonString());
        return report["passed"]?.GetValue<bool>() == true;
    }
}
