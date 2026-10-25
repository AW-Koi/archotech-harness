namespace Scenarios;

/// <summary>A deliberate Log.Error fails a scenario unless [AllowGameErrors] accepts it, as it does here.</summary>
[AllowGameErrors(DeliberateError)]
public sealed class AllowedGameError : HarnessScenario
{
    private const string DeliberateError = "Deliberate error from the AllowedGameError sample";

    public override Task Run()
    {
        // The scenario logs the same error twice and closes the log window it opens
        Log.Error(DeliberateError);
        Log.Error(DeliberateError);
        Find.WindowStack.TryRemove(typeof(LudeonTK.EditWindow_Log), doCloseSound: false);

        // Both errors count, and only the allow list lets them pass
        var errors = ScriptSession.Current!.GameErrors;
        Check(errors.Distinct.Single(e => e.Text == DeliberateError).Count == 2);
        Check(errors.FailureMessage(GameErrorAllowList.None) != null);
        Check(errors.FailureMessage(ScriptSession.Current.AllowedErrors) == null);
        return Task.CompletedTask;
    }
}
