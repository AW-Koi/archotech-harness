using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Verse;

namespace DevHarness.Probes;

/// <summary>Registers public static methods of any class named DevHarnessProbes in a loaded mod.</summary>
/// <remarks>Methods take no argument or one JSON string; each registers as "AssemblyName.MethodName".</remarks>
public static class ProbeDiscovery
{
    public const string ConventionTypeName = "DevHarnessProbes";

    public static void RegisterConventionProbes()
    {
        var types = LoadedModManager.RunningMods
            .SelectMany(mod => mod.assemblies.loadedAssemblies)
            .SelectMany(SafeTypes)
            .Where(t => t.Name == ConventionTypeName);
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                var takesArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                if (parameters.Length > 1 || (parameters.Length == 1 && !takesArgs) || method.ReturnType == typeof(void))
                {
                    continue;
                }

                var name = $"{type.Assembly.GetName().Name}.{method.Name}";
                HarnessProbes.Register(name, args => ToJson(method.Invoke(null, takesArgs ? new object[] { args.ToJsonString() } : null)));
                Log.Message($"[DevHarness] Registered probe {name}.");
            }
        }
    }

    private static JsonNode ToJson(object value) => value switch
    {
        null => null,
        string text => HarnessProbes.ParseResult(text),
        _ => JsonSerializer.SerializeToNode(value, value.GetType()),
    };

    private static Type[] SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null).ToArray();
        }
    }
}
