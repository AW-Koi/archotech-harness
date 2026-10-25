using System.Text.Json.Nodes;
using ArchotechHarness.Shared;

namespace ArchotechHarness.Cli.Tests;

public class JsonPathTests
{
    private const string PawnResult = """{"pawn":{"name":"Mopper","cell":[121,122]},"count":7}""";

    [Fact]
    public void Select_NestedField_ReturnsValue()
    {
        var root = JsonNode.Parse(PawnResult);

        var selected = JsonPath.Select(root, "pawn.name");

        Assert.Equal("Mopper", selected!.GetValue<string>());
    }

    [Fact]
    public void Select_ArrayIndex_ReturnsElement()
    {
        var root = JsonNode.Parse(PawnResult);

        var selected = JsonPath.Select(root, "pawn.cell.1");

        Assert.Equal(122, selected!.GetValue<int>());
    }

    [Fact]
    public void Select_MissingField_ReturnsNull()
    {
        var root = JsonNode.Parse(PawnResult);

        var selected = JsonPath.Select(root, "pawn.job");

        Assert.Null(selected);
    }

    [Fact]
    public void Select_EmptyPath_ReturnsRoot()
    {
        var root = JsonNode.Parse(PawnResult);

        var selected = JsonPath.Select(root, "");

        Assert.Same(root, selected);
    }
}
