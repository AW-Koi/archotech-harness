using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ArchotechHarness.Scripting;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Loads a built scenarios assembly and runs one HarnessScenario's Run() on the main thread.</summary>
/// <remarks>Arguments: "assembly" (path), "entryType" (full class name), "outputDirectory". The game is paused before Run() starts, so the quick-test colonists stand still. Replies with the report when Run() ends.</remarks>
public static class RunScenarioCommand
{
    private static readonly TimeSpan PauseTimeout = TimeSpan.FromSeconds(15);

    public static CommandResult Run(JsonObject args)
    {
        if (Find.CurrentMap == null && MapGenerationFailure.Last is { } failure)
        {
            throw new HarnessException($"No map is loaded because the last one failed to generate: {failure}");
        }

        var map = Arguments.CurrentMap();
        ScriptSession.Current?.End();
        var assembly = Assembly.LoadFrom(Arguments.RequiredString(args, "assembly"));
        DefOfBinder.Bind(DefOfBinder.DefOfClasses(assembly));
        var typeName = Arguments.RequiredString(args, "entryType");
        var type = assembly.GetType(typeName) ?? throw new HarnessException($"The scenarios assembly has no type {typeName}.");
        if (!typeof(HarnessScenario).IsAssignableFrom(type))
        {
            throw new HarnessException($"{typeName} does not derive from {nameof(HarnessScenario)}.");
        }

        if (ScenarioBiome.Resolve(type) is { } biome && map.Biome != biome)
        {
            throw new HarnessException($"{type.Name} asks for [Biome(\"{biome.defName}\")], but this colony is on {map.Biome.defName}; run it with archo, which starts games on that biome.");
        }

        var session = ScriptSession.Begin(Arguments.RequiredString(args, "outputDirectory"));
        session.AllowedErrors = GameErrorAllowList.From(type.GetCustomAttributes<AllowGameErrorsAttribute>());
        if (Arguments.Float(args, "recordFps", 0) is var fps and > 0)
        {
            session.StartRecording(System.IO.Path.Combine(session.OutputDirectory, "frames"), fps);
        }

        session.HeldSpeed = TimeSpeed.Paused;
        var pauseDeadline = DateTime.UtcNow + PauseTimeout;
        Task scenario = null;

        return CommandResult.Pending(() =>
        {
            if (scenario == null)
            {
                if (Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && DateTime.UtcNow < pauseDeadline)
                {
                    return null;
                }

                scenario = Start(type);
            }

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

    private static Task Start(Type type)
    {
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
        return scenario;
    }

    private static JsonArray DeclaredMods(Type type) =>
        new(type.GetCustomAttributes<ModAttribute>()
            .Select(mod => (JsonNode)new JsonObject { ["dir"] = mod.Directory, ["build"] = mod.Build, ["buildIn"] = mod.BuildIn })
            .ToArray());
}
