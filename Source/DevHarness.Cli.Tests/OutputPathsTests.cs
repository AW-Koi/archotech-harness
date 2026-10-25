using System.Text.Json.Nodes;

namespace DevHarness.Cli.Tests;

public class OutputPathsTests
{
    private const string RunDirectory = @"C:\runs\mop-test";

    [Theory]
    [InlineData("screenshot")]
    [InlineData("windowCapture")]
    public void Resolve_RelativeFileForACaptureCommand_LandsInTheRunFolder(string command)
    {
        var args = new JsonObject { ["path"] = "before.png" };

        OutputPaths.Resolve(command, args, RunDirectory);

        Assert.Equal(@"C:\runs\mop-test\before.png", args["path"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_AbsoluteFileForACaptureCommand_StaysAsGiven()
    {
        var args = new JsonObject { ["path"] = @"D:\shots\before.png" };

        OutputPaths.Resolve("screenshot", args, RunDirectory);

        Assert.Equal(@"D:\shots\before.png", args["path"]!.GetValue<string>());
    }

    [Fact]
    public void Resolve_ResultPathOfAWait_StaysAsGiven()
    {
        var args = new JsonObject { ["path"] = "count" };

        OutputPaths.Resolve("wait", args, RunDirectory);

        Assert.Equal("count", args["path"]!.GetValue<string>());
    }
}
