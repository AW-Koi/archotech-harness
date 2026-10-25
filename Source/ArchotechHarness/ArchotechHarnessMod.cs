using ArchotechHarness.Commands;
using ArchotechHarness.Probes;
using ArchotechHarness.Server;
using HarmonyLib;
using Verse;

namespace ArchotechHarness;

/// <summary>Starts the command server and installs the main-thread pump.</summary>
public sealed class ArchotechHarnessMod : Mod
{
    public const string DefaultPort = "47801";

    public ArchotechHarnessMod(ModContentPack content) : base(content)
    {
        new Harmony("TerraIncognita.ArchotechHarness").PatchAll(typeof(ArchotechHarnessMod).Assembly);
        var port = GenCommandLine.TryGetCommandLineArg("harnessPort", out var value) ? value : DefaultPort;
        StartingTile.Biome = GenCommandLine.TryGetCommandLineArg("harnessBiome", out var biome) ? biome : null;
        CommandServer.Start(int.Parse(port));
        LongEventHandler.ExecuteWhenFinished(ProbeDiscovery.RegisterConventionProbes);
        Log.Message($"[ArchotechHarness] Listening on 127.0.0.1:{port}.");
    }
}
