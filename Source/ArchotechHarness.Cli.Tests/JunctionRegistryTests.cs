namespace ArchotechHarness.Cli.Tests;

public sealed class JunctionRegistryTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Add_SameLinkTwice_IsRecordedOnce()
    {
        var registry = _temp.Combine("junctions.json");

        JunctionRegistry.Add(registry, @"Mods\archo-mopmod");
        JunctionRegistry.Add(registry, @"Mods\archo-mopmod");
        JunctionRegistry.Add(registry, @"Mods\archo-broommod");

        Assert.Equal([@"Mods\archo-mopmod", @"Mods\archo-broommod"], JunctionRegistry.Load(registry));
    }

    [Fact]
    public void Load_NoRegistryFile_IsEmpty()
    {
        Assert.Empty(JunctionRegistry.Load(_temp.Combine("junctions.json")));
    }
}
