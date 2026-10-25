using ArchotechHarness.Scripting;

namespace ArchotechHarness.Tests;

public sealed class PendingBuildersTests
{
    private const string ColonistBuilder = "Spawn.Colonist() at Cleaning.cs:12 ends with .Spawn()";
    private const string RoomBuilder = "Build.Room() at Cleaning.cs:9 ends with .Build()";

    [Fact]
    public void PendingBuilders_WhenABuilderIsNeverFinished_ListIt()
    {
        var builders = new PendingBuilders();

        builders.Track(ColonistBuilder);

        Assert.Equal([ColonistBuilder], builders.Unfinished);
    }

    [Fact]
    public void PendingBuilders_WhenABuilderFinishes_DropIt()
    {
        var builders = new PendingBuilders();
        var room = builders.Track(RoomBuilder);
        builders.Track(ColonistBuilder);

        builders.Finish(room);

        Assert.Equal([ColonistBuilder], builders.Unfinished);
    }

    [Fact]
    public void PendingBuilders_WhenTheSameBuilderFinishesTwice_StayEmpty()
    {
        var builders = new PendingBuilders();
        var room = builders.Track(RoomBuilder);

        builders.Finish(room);
        builders.Finish(room);

        Assert.Empty(builders.Unfinished);
    }
}
