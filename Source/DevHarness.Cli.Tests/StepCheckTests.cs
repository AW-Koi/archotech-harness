using System.Text.Json.Nodes;

namespace DevHarness.Cli.Tests;

public class StepCheckTests
{
    private const string FilthResult = """{"count":4,"byDef":{"Filth_Blood":4}}""";

    [Fact]
    public void Expectation_MetByTheResultField_Passes()
    {
        var expectation = new StepExpectation("count", "lt", JsonValue.Create(10));

        var failure = StepCheck.Failure(JsonNode.Parse(FilthResult), expectation);

        Assert.Null(failure);
    }

    [Fact]
    public void Expectation_NotMet_DescribesTheActualValue()
    {
        var expectation = new StepExpectation("byDef.Filth_Blood", "eq", JsonValue.Create(0));

        var failure = StepCheck.Failure(JsonNode.Parse(FilthResult), expectation);

        Assert.Equal("expected byDef.Filth_Blood eq 0, got 4", failure);
    }

    [Fact]
    public void Expectation_Absent_Passes()
    {
        var failure = StepCheck.Failure(JsonNode.Parse(FilthResult), null);

        Assert.Null(failure);
    }
}
