using System.Threading.Tasks;

namespace DevHarness.Scripting;

/// <summary>A scenario rwh can run: Run() executes on the game's main thread once a map is loaded.</summary>
/// <remarks>Describe the profile with [Mod], [Dlc] and [Window] on the class. Await the Script helpers; never block.</remarks>
public abstract class HarnessScenario
{
    public abstract Task Run();
}
