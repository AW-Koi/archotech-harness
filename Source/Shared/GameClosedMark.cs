namespace ArchotechHarness.Shared;

/// <summary>The file the game leaves in the run folder when it closes normally mid-scenario, so archo can tell a closed game
/// from a crash.</summary>
public static class GameClosedMark
{
    public const string FileName = "game-closed";
}
