namespace Scenarios;

/// <summary>A raid that can't fire says which step failed.</summary>
public sealed class RaidSteps : HarnessScenario
{
    public override Task Run()
    {
        // A raid from a faction at peace with the colony fails at its faction
        World.Calm();
        var friendly = Find.FactionManager.AllFactionsVisible.First(faction => !faction.IsPlayer && !faction.HostileTo(Faction.OfPlayer));
        RaidStep? failedAt = null;
        try
        {
            Spawn.Raid(500).From(friendly).Spawn();
        }
        catch (RaidException e)
        {
            failedAt = e.Step;
        }

        Check(failedAt == RaidStep.Faction);
        return Task.CompletedTask;
    }
}
