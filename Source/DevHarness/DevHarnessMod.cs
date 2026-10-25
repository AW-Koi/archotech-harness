using DevHarness.Probes;
using DevHarness.Server;
using HarmonyLib;
using Verse;

namespace DevHarness;

/// <summary>Starts the command server and installs the main-thread pump.</summary>
public sealed class DevHarnessMod : Mod
{
    public const string DefaultPort = "47801";

    public DevHarnessMod(ModContentPack content) : base(content)
    {
        new Harmony("TerraIncognita.RimWorldDevHarness").PatchAll(typeof(DevHarnessMod).Assembly);
        var port = GenCommandLine.TryGetCommandLineArg("harnessPort", out var value) ? value : DefaultPort;
        CommandServer.Start(int.Parse(port));
        LongEventHandler.ExecuteWhenFinished(ProbeDiscovery.RegisterConventionProbes);
        Log.Message($"[DevHarness] Listening on 127.0.0.1:{port}.");
    }
}
