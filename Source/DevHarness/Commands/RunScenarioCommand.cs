using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DevHarness.Scripting;

namespace DevHarness.Commands;

/// <summary>Loads a built scenarios assembly and runs one HarnessScenario's Run() on the main thread.</summary>
/// <remarks>Arguments: "assembly" (path), "entryType" (full class name), "outputDirectory". Replies with the run report when Run() ends.</remarks>
public static class RunScenarioCommand
{
    public static CommandResult Run(JsonObject args)
    {
        Arguments.CurrentMap();
        ScriptSession.Current?.End();
        var assembly = Assembly.LoadFrom(Arguments.RequiredString(args, "assembly"));
        var typeName = Arguments.RequiredString(args, "entryType");
        var type = assembly.GetType(typeName) ?? throw new HarnessException($"The scenarios assembly has no type {typeName}.");
        if (!typeof(HarnessScenario).IsAssignableFrom(type))
        {
            throw new HarnessException($"{typeName} does not derive from {nameof(HarnessScenario)}.");
        }

        var session = ScriptSession.Begin(Arguments.RequiredString(args, "outputDirectory"));
        Task scenario = null;
        MainThreadContext.Instance.Run(() =>
        {
            try
            {
                scenario = ((HarnessScenario)Activator.CreateInstance(type)!).Run();
            }
            catch (Exception e)
            {
                scenario = Task.FromException(e is TargetInvocationException { InnerException: { } inner } ? inner : e);
            }
        });

        return CommandResult.Pending(() =>
        {
            if (!scenario.IsCompleted)
            {
                return null;
            }

            var report = session.Report(scenario);
            report["mods"] = DeclaredMods(type);
            session.End();
            return report;
        });
    }

    private static JsonArray DeclaredMods(Type type) =>
        new(type.GetCustomAttributes<ModAttribute>()
            .Select(mod => (JsonNode)new JsonObject { ["dir"] = mod.Directory, ["build"] = mod.Build, ["buildIn"] = mod.BuildIn })
            .ToArray());
}
