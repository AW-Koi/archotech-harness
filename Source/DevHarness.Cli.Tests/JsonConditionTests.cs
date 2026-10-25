using System.Text.Json.Nodes;
using DevHarness.Shared;

namespace DevHarness.Cli.Tests;

public class JsonConditionTests
{
    [Theory]
    [InlineData("3", "lt", "5", true)]
    [InlineData("5", "lt", "5", false)]
    [InlineData("5", "le", "5", true)]
    [InlineData("6", "gt", "5", true)]
    [InlineData("5", "ge", "6", false)]
    [InlineData("5", "eq", "5.0", true)]
    [InlineData("\"Clean\"", "eq", "\"Clean\"", true)]
    [InlineData("\"Clean\"", "ne", "\"Wait\"", true)]
    [InlineData("true", "eq", "true", true)]
    public void Comparison_WithOperator_MatchesExpectation(string actual, string op, string expected, bool holds)
    {
        var result = JsonCondition.Holds(JsonNode.Parse(actual), op, JsonNode.Parse(expected));

        Assert.Equal(holds, result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("4", true)]
    public void Comparison_WithoutOperator_TestsTruthiness(string actual, bool holds)
    {
        var result = JsonCondition.Holds(JsonNode.Parse(actual), null, null);

        Assert.Equal(holds, result);
    }

    [Fact]
    public void Comparison_MissingActualValue_DoesNotHold()
    {
        var result = JsonCondition.Holds(null, "lt", JsonValue.Create(5));

        Assert.False(result);
    }

    [Fact]
    public void Comparison_UnknownOperator_Throws()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => JsonCondition.Holds(JsonValue.Create(1), "approx", JsonValue.Create(1)));
    }
}
