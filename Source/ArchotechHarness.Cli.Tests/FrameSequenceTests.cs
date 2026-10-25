namespace ArchotechHarness.Cli.Tests;

public sealed class FrameSequenceTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Renumber_FramesWithGapsAndAnEmptyFile_MakesAGapFreeSequence()
    {
        _temp.WriteFile("frames/frame_00000.png", "a");
        _temp.WriteFile("frames/frame_00002.png", "b");
        _temp.WriteFile("frames/frame_00003.png", "");
        _temp.WriteFile("frames/frame_00007.png", "c");

        var count = FrameSequence.Renumber(_temp.Combine("frames"));

        Assert.Equal(3, count);
        Assert.Equal(["seq_00000.png", "seq_00001.png", "seq_00002.png"],
            Directory.GetFiles(_temp.Combine("frames"), "seq_*.png").Select(Path.GetFileName).Order());
        Assert.Equal("c", File.ReadAllText(_temp.Combine("frames", "seq_00002.png")));
    }

    [Fact]
    public void Renumber_MissingFolder_CountsNothing()
    {
        Assert.Equal(0, FrameSequence.Renumber(_temp.Combine("no-frames")));
    }
}
