using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DevHarness.Scripting;

namespace DevHarness.Commands;

/// <summary>Loads a compiled scenario script and runs its async entry method on the main thread.</summary>
/// <remarks>Arguments: "assembly" (path), "entryType", "outputDirectory". Replies with the run report when the script ends.</remarks>
public static class RunScriptCommand
{
    public const string EntryMethod = "Run";

    public static CommandResult Run(JsonObject args)
    {
        Arguments.CurrentMap();
        ScriptSession.Current?.End();
        var assembly = Assembly.LoadFrom(Arguments.RequiredString(args, "assembly"));
        var entry = assembly.GetType(Arguments.RequiredString(args, "entryType"))?.GetMethod(EntryMethod)
            ?? throw new HarnessException($"The script assembly has no {EntryMethod} method.");
        var session = ScriptSession.Begin(Arguments.RequiredString(args, "outputDirectory"));

        Task script = null;
        MainThreadContext.Instance.Run(() =>
        {
            try
            {
                script = (Task)entry.Invoke(null, null);
            }
            catch (TargetInvocationException e)
            {
                script = Task.FromException(e.InnerException ?? e);
            }
        });

        return CommandResult.Pending(() =>
        {
            if (!script.IsCompleted)
            {
                return null;
            }

            var report = session.Report(script);
            session.End();
            return report;
        });
    }
}
