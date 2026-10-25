using System;
using System.Text.Json.Nodes;
using Verse;

namespace ArchotechHarness.Commands;

/// <summary>Sets the game speed: paused, normal, fast, superfast or ultrafast.</summary>
/// <remarks>Retries each frame until the game hands the player control, as it does a few ticks into a new map.</remarks>
public static class SpeedCommand
{
    private const double TimeoutSeconds = 15;

    public static CommandResult Run(JsonObject args)
    {
        Arguments.CurrentMap();
        var speed = Enum.Parse<TimeSpeed>(Arguments.String(args, "speed", "normal"), ignoreCase: true);
        var deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
        return CommandResult.Pending(() =>
        {
            Find.TickManager.CurTimeSpeed = speed;
            if (Find.TickManager.CurTimeSpeed == speed)
            {
                return new JsonObject { ["speed"] = speed.ToString(), ["tick"] = Find.TickManager.TicksGame };
            }

            return DateTime.UtcNow > deadline ? throw new HarnessException("The game did not hand over control of the speed.") : null;
        });
    }
}
