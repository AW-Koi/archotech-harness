using System.Diagnostics;

namespace ArchotechHarness.Cli;

/// <summary>Builds a scenarios project under a unique assembly name, so a running game can load each build.</summary>
public static class ScenarioProjectBuilder
{
    /// <summary>Builds the project into binDirectory and returns the assembly path; throws with the compiler errors on failure.</summary>
    public static string Build(string project, string binDirectory)
    {
        var assemblyName = "Scenarios_" + Guid.NewGuid().ToString("N");
        var start = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { "build", project, "--nologo", "-v", "q", "-clp:NoSummary" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["ARCHO_SCENARIO_ASSEMBLY"] = assemblyName;
        start.Environment["ARCHO_SCENARIO_OUTDIR"] = binDirectory;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return Path.Combine(binDirectory, assemblyName + ".dll");
        }

        var lines = (stdout.Result + stderr.Result).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var errors = lines.Where(l => l.Contains(" error ")).Distinct().ToList();
        throw new ScenarioBuildException(project, errors.Count > 0 ? errors : lines);
    }
}
