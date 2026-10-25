namespace Scenarios;

/// <summary>A 200-colonist crowd at superfast: measures ticks per second, attaches a transcript, and draws seeded random numbers.</summary>
[Seed(42)]
public sealed class ScaleCheck : HarnessScenario
{
    private const int Crowd = 200;
    private const int FirstDrawForSeed42 = 953028511;

    public override async Task Run()
    {
        // The seed restarts the game's random numbers just before Run
        Check(Rand.Int == FirstDrawForSeed42);

        // Two hundred colonists crowd a cleared field
        Clock.Speed = TimeSpeed.Paused;
        World.Calm();
        World.RemoveHostiles();
        var c = World.Center;
        var field = new CellRect(c.x - 20, c.z - 20, 41, 41);
        Build.Clearing(field);
        var started = DateTime.UtcNow;
        var crowd = Spawn.Colonists(Crowd).Within(field).Spawn();
        Colonists.RemoveAllExcept(crowd.ToArray());
        Note("spawnSeconds", Math.Round((DateTime.UtcNow - started).TotalSeconds, 1));

        // Every colonist stands in the field
        Check(crowd.Count == Crowd);
        Check(Colonists.All.Count == Crowd);
        Check(crowd.All(p => field.Contains(p.Position)));
        Camera.Frame(field);
        await Shot("crowd");

        // The crowd runs at superfast for ten seconds while the harness counts ticks and frames
        Clock.Speed = TimeSpeed.Superfast;
        var tps = await TicksPerSecond(10);
        Clock.Speed = TimeSpeed.Paused;
        Check(tps > 0);

        // A transcript a mod might write lands in the report
        var transcript = AttachText("transcript.txt", string.Join(Environment.NewLine, crowd.Take(5).Select(p => $"{p.LabelShort}: Crowded in here.")));
        Check(System.IO.File.ReadAllText(transcript).StartsWith(crowd[0].LabelShort));
    }
}
