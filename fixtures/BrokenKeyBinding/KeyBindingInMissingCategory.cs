namespace Fixtures;

/// <summary>Loads a mod whose key binding names a missing category; archo must fail the run before this scenario counts.</summary>
public sealed class KeyBindingInMissingCategory : HarnessScenario
{
    public override Task Run()
    {
        // The colony loads, so only the load errors can fail the run
        Check(Find.CurrentMap != null);
        return Task.CompletedTask;
    }
}
