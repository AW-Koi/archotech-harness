using System;
using System.Text.Json.Nodes;
using ArchotechHarness.Scripting;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Profile;

namespace ArchotechHarness.Commands;

/// <summary>Throws the current game away and starts a fresh quick-test colony, as the -quicktest start does.</summary>
/// <remarks>Arguments: "mapSize" (default 250), "planetCoverage" (default 0.05, or 0.3 with a biome), "seed", "biome" (a BiomeDef name; default any). Replies once the new map is playable.</remarks>
public static class NewGameCommand
{
    private const double TimeoutSeconds = 180;

    public static CommandResult Run(JsonObject args)
    {
        ScriptSession.Current?.End();
        var mapSize = Arguments.Int(args, "mapSize", 250);
        StartingTile.Biome = Arguments.String(args, "biome");
        var coverage = Arguments.Float(args, "planetCoverage", StartingTile.Biome == null ? 0.05f : 0.3f);
        var seed = Arguments.String(args, "seed") ?? GenText.RandomSeedString();
        MapGenerationFailure.Last = null;
        var previous = Current.Game;
        var started = DateTime.UtcNow;

        LongEventHandler.ClearQueuedEvents();
        previous?.Dispose();
        LongEventHandler.QueueLongEvent(() =>
        {
            MemoryUtility.ClearAllMapsAndWorld();
            Setup(mapSize, coverage, seed);
            Find.GameInitData.PrepForMapGen();
            Find.Scenario.PreMapGenerate();
        }, "Play", "GeneratingMap", doAsynchronously: true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);

        return CommandResult.Pending(() =>
        {
            var ready = Current.Game != null && Current.Game != previous && Current.ProgramState == ProgramState.Playing
                && Find.CurrentMap != null && !LongEventHandler.AnyEventNowOrWaiting;
            if (ready)
            {
                return new JsonObject
                {
                    ["seconds"] = Math.Round((DateTime.UtcNow - started).TotalSeconds, 2),
                    ["seed"] = seed,
                    ["mapSize"] = new JsonArray(Find.CurrentMap.Size.x, Find.CurrentMap.Size.z),
                };
            }

            if (MapGenerationFailure.Last is { } failure)
            {
                throw new HarnessException($"The new map failed to generate: {failure}");
            }

            return DateTime.UtcNow - started > TimeSpan.FromSeconds(TimeoutSeconds)
                ? throw new HarnessException("The new game did not finish loading.")
                : null;
        });
    }

    private static void Setup(int mapSize, float coverage, string seed)
    {
        Current.ProgramState = ProgramState.Entry;
        Game.ClearCaches();
        Current.Game = new Game { InitData = new GameInitData() };
        Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
        Find.Scenario.PreConfigure();
        Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
        Current.Game.World = WorldGenerator.GenerateWorld(coverage, seed, OverallRainfall.Normal, OverallTemperature.Normal,
            OverallPopulation.Normal, LandmarkDensity.Normal);
        Find.GameInitData.ChooseRandomStartingTile();
        Find.GameInitData.mapSize = mapSize;
        Find.Scenario.PostIdeoChosen();
    }
}
